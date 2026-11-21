import React from 'react';

/** Splits text into segments, auto-linking emails and URLs */
function parseContent(text: string): React.ReactNode[] {
  // Regex for emails and URLs
  const emailRegex = /\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b/g;
  const urlRegex = /\b(?:https?:\/\/|www\.)[^\s<>"']+/g;
  const combinedRegex = new RegExp(`(${emailRegex.source})|(${urlRegex.source})`, 'g');

  const parts = text.split(combinedRegex);
  const nodes: React.ReactNode[] = [];

  for (let i = 0; i < parts.length; i++) {
    const part = parts[i];
    if (!part) continue;

    if (emailRegex.test(part)) {
      nodes.push(
        <a
          key={`email-${i}`}
          href={`mailto:${part}`}
          target="_blank"
          rel="noopener noreferrer"
          className="message-link"
        >
          {part}
        </a>
      );
    } else if (urlRegex.test(part)) {
      const href = part.startsWith('http') ? part : `https://${part}`;
      nodes.push(
        <a
          key={`url-${i}`}
          href={href}
          target="_blank"
          rel="noopener noreferrer"
          className="message-link"
        >
          {part}
        </a>
      );
    } else {
      nodes.push(part);
    }
  }

  return nodes;
}

/** Message content with auto-linked emails and URLs */
export default function MessageContent({ children }: { children: string }) {
  return <span className="message-content">{parseContent(children)}</span>;
}