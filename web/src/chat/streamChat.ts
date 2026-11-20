export type Role = 'user' | 'assistant';

export interface ChatTurn {
  role: Role;
  content: string;
}

export interface StreamHandlers {
  onDelta: (text: string) => void;
  onSources?: (ids: string[]) => void;
}

/** An error whose message is safe to show to the visitor as-is. */
export class ChatError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
  ) {
    super(message);
  }
}

// Empty in dev (Vite proxies /api). Set VITE_API_URL at build time for production.
const API_URL: string = import.meta.env.VITE_API_URL ?? '';

/**
 * POSTs the conversation and reads the Server-Sent Events response.
 * EventSource can't be used here because it only supports GET.
 * Events: sources (chunk ids), delta (text fragment), done, error.
 */
export async function streamChat(
  messages: ChatTurn[],
  handlers: StreamHandlers,
  signal: AbortSignal,
): Promise<void> {
  let res: Response;
  try {
    res = await fetch(`${API_URL}/api/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ messages }),
      signal,
    });
  } catch (e) {
    if (signal.aborted) throw e;
    throw new ChatError("Couldn't reach the assistant. Check your connection and try again.");
  }

  if (res.status === 429) {
    throw new ChatError('Too many questions in a short time. Wait a minute and try again.', 429);
  }
  if (res.status === 400) {
    throw new ChatError('That message was too long or the conversation got too big. Start a new chat.', 400);
  }
  if (!res.ok || !res.body) {
    throw new ChatError('The assistant is unavailable right now. Try again shortly.', res.status);
  }

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    // Events are separated by a blank line.
    let boundary: number;
    while ((boundary = buffer.indexOf('\n\n')) !== -1) {
      const raw = buffer.slice(0, boundary);
      buffer = buffer.slice(boundary + 2);
      dispatch(raw, handlers);
    }
  }
}

function dispatch(raw: string, handlers: StreamHandlers): void {
  let event = 'message';
  const data: string[] = [];

  for (const line of raw.split('\n')) {
    if (line.startsWith('event:')) event = line.slice(6).trim();
    else if (line.startsWith('data:')) data.push(line.slice(5).trimStart());
  }
  if (data.length === 0) return;

  const payload: unknown = JSON.parse(data.join('\n'));

  switch (event) {
    case 'delta':
      handlers.onDelta(String(payload));
      break;
    case 'sources':
      handlers.onSources?.(payload as string[]);
      break;
    case 'error':
      throw new ChatError(String(payload));
  }
}