import { useState, useEffect } from 'react';
import { cv } from './cv';
import { downloadCvPdf } from './CvPdf';

const SECTION_DELAY_MS = 180; // ms between section reveals

export default function CvView() {
  const [downloading, setDownloading] = useState(false);
  const [visibleSections, setVisibleSections] = useState<Set<number>>(new Set());

  const handleDownload = async () => {
    if (downloading) return;
    setDownloading(true);
    try {
      await downloadCvPdf();
    } catch {
      alert("Sorry, couldn't generate the PDF. Try again?");
    } finally {
      setDownloading(false);
    }
  };

  // Stagger section reveals on mount
  useEffect(() => {
    const totalSections = 2 + cv.experience.length + 4; // header + summary + experience groups + education + certs + skills + languages
    const timers: ReturnType<typeof setTimeout>[] = [];
    const timer = setTimeout(() => {
      for (let i = 0; i < totalSections; i++) {
        const innerTimer = setTimeout(() => setVisibleSections((prev) => new Set([...prev, i])), i * SECTION_DELAY_MS);
        timers.push(innerTimer);
      }
    }, 50); // small initial delay
    return () => {
      clearTimeout(timer);
      timers.forEach((t) => clearTimeout(t));
    };
  }, []);

  const sections = [
    // Header (index 0)
    (
      <header key="header" className="cv-head">
        <h2>{cv.name}</h2>
        <p>{cv.headline}</p>
        <p className="cv-contact">
          {cv.location}
          <br />
          <a href={cv.links.github} target="_blank" rel="noreferrer">GitHub</a>{' '}
          <a href={cv.links.linkedin} target="_blank" rel="noreferrer">LinkedIn</a>{' '}
          <a href={`mailto:${cv.links.email}`}>Email</a>
        </p>
        <button
          className="cv-download"
          onClick={handleDownload}
          type="button"
          disabled={downloading}
          aria-busy={downloading}
        >
          {downloading ? 'Generating…' : 'Download PDF'}
        </button>
      </header>
    ),
    // Summary (index 1)
    (
      <section key="summary">
        <h3>Summary</h3>
        <p>{cv.summary}</p>
      </section>
    ),
    // Experience groups (indices 2+)
    ...cv.experience.map((group) => (
      <section key={group.heading}>
        <h3>{group.heading}</h3>
        {group.roles.map((r) => (
          <div className="cv-item" key={`${r.org}-${r.period}`}>
            <div className="cv-row">
              <p>
                <strong>{r.title}</strong>, {r.org}
              </p>
              <span>{r.period}</span>
            </div>
            <ul>
              {r.bullets.map((b) => (
                <li key={b}>{b}</li>
              ))}
            </ul>
          </div>
        ))}
      </section>
    )),
    // Education
    (
      <section key="education">
        <h3>Education</h3>
        {cv.education.map((e) => (
          <div className="cv-row cv-item" key={e.title}>
            <p>
              <strong>{e.title}</strong>, {e.org}
            </p>
            <span>{e.period}</span>
          </div>
        ))}
      </section>
    ),
    // Certification
    (
      <section key="certification">
        <h3>Certification</h3>
        {cv.certifications.map((c) => (
          <div className="cv-row cv-item" key={c.title}>
            <p>
              <strong>{c.title}</strong>, {c.org}
            </p>
            <span>{c.period}</span>
          </div>
        ))}
      </section>
    ),
    // Skills
    (
      <section key="skills">
        <h3>Skills</h3>
        <dl className="cv-skills">
          {cv.skills.map((s) => (
            <div key={s.label}>
              <dt>{s.label}</dt>
              <dd>{s.items.join(', ')}</dd>
            </div>
          ))}
        </dl>
      </section>
    ),
    // Languages
    (
      <section key="languages">
        <h3>Languages</h3>
        <p>{cv.languages}</p>
      </section>
    ),
  ];

  return (
    <article className="cv" aria-label={`${cv.name}'s CV`}>
      {sections.map((section, index) => (
        <div
          key={section.key}
          className={`cv-section ${visibleSections.has(index) ? 'visible' : ''}`}
        >
          {section}
        </div>
      ))}
    </article>
  );
}