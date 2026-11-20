// The CV shown when a visitor asks to see it. Every line comes from the chunk files in data/.
// If you change the chunks, update this file too (the chat answers from the chunks, this page does not).

export interface Role {
  title: string;
  org: string;
  period: string;
  bullets: string[];
}

export interface RoleGroup {
  heading: string;
  roles: Role[];
}

export const cv = {
  name: 'Felipe Berger',
  headline: 'Backend software engineer',
  location: 'Canberra, ACT, Australia',
  links: {
    github: 'https://github.com/felipeberger',
    linkedin: 'https://linkedin.com/in/felipe-berger',
    email: 'felipecberger@gmail.com',
  },
  summary:
    'Backend software engineer with 4+ years of experience designing, building and maintaining backend services, APIs, third-party integrations and data-intensive systems. Strongest in Ruby on Rails, PostgreSQL and production debugging, with a focus on reliability, performance, testing and maintainable architecture. Has supported production systems used by 150k+ users and worked directly with users and stakeholders to solve complex problems.',

  experience: [
    {
      heading: 'Software engineering',
      roles: [
        {
          title: 'Backend Developer',
          org: 'CardCastle, Australia',
          period: 'June 2022 – May 2026',
          bullets: [
            'Designed and maintained Rails backend services and APIs supporting 150k+ users across B2C and B2B platforms.',
            'Redesigned a core data-export system, improving performance by over 70% for large exports and fixing out-of-memory failures on very large jobs.',
            'Reduced API response times from over 1.5 seconds to under 0.5 seconds by refactoring legacy SQL, fixing N+1 queries and adding indexes.',
            'Helped migrate a daily background job to a Rust microservice, cutting its execution time by 66%.',
            'Refactored a 10-year-old mission-critical codebase and built a FactoryBot framework of mock pricing payloads, so the price-update workflow could be tested automatically for the first time.',
            'Helped design and build an inventory management system, combining customer research with engineering requirements.',
            'Acted as technical support for escalated production issues: investigate logs and Datadog, replicate the error, ship a tested fix.',
          ],
        },
        {
          title: 'Full-stack Software Engineer (internship, remote)',
          org: 'betterwritter.ai',
          period: 'March – May 2022',
          bullets: [
            'Increased test coverage from 60% to 85%.',
            'Refactored React components and Rails backend logic into modular, reusable components following OOP principles.',
          ],
        },
        {
          title: 'Junior Software Engineer',
          org: 'Relive Digital, Australia',
          period: 'July 2021 – May 2022',
          bullets: [
            'Built GraphQL resolvers with AWS Lambda and AppSync for a serverless backend.',
            'Created UI mockups in Figma and implemented frontend components in Vue.js.',
          ],
        },
      ],
    },
    {
      heading: 'Earlier career',
      roles: [
        {
          title: 'Consultant, then Senior Consultant',
          org: 'Emerging Markets Consulting, Cambodia and Laos',
          period: 'April 2013 – June 2021',
          bullets: [
            'Led over 35 projects and a team of four across Southeast Asia for government, business and NGO clients, including market entry studies, market sizing, regulatory assessments, feasibility studies and monitoring and evaluation.',
            'Ran in-depth interviews, focus groups and co-creation workshops with business owners and service users.',
            'Wrote winning proposals and pitched to clients in Cambodia, Lao PDR and Myanmar.',
            'Subject matter expert on solar energy and on labour market assessments.',
          ],
        },
        {
          title: 'Regulatory Specialist',
          org: 'QPharma, New Jersey, USA',
          period: 'January 2012 – February 2013',
          bullets: [
            'Led marketing compliance for pharmaceutical and nutritional supplement clients and drafted legal memos on compliance issues.',
            'Led trademark prosecution, successfully registering two trademarks with the USPTO.',
          ],
        },
        {
          title: 'Regulatory Analyst',
          org: 'Porzio Life Sciences, New Jersey, USA',
          period: 'January – September 2010',
          bullets: [
            'Led regulatory research and built new regulatory databases, increasing the company’s offerings.',
            'Managed the legislative tracking database covering new and proposed laws in all 51 US states.',
          ],
        },
      ],
    },
  ] satisfies RoleGroup[],

  education: [
    {
      title: 'Programming and Full-stack Web Development Bootcamp',
      org: 'Altcademy',
      period: 'July 2020 – November 2021',
    },
    {
      title: 'Master of Science in Jurisprudence, Intellectual Property Law',
      org: 'Seton Hall School of Law, USA',
      period: 'September 2010 – December 2011',
    },
    {
      title: 'Bachelor of Arts in Psychology, minor in Entrepreneurship',
      org: 'St John’s University, USA',
      period: 'September 2005 – May 2009',
    },
  ],

  certifications: [
    { title: 'Project Management Professional (PMP)', org: 'Project Management Institute', period: 'December 2018' },
  ],

  skills: [
    { label: 'Backend and data', items: ['Ruby on Rails', 'Rust', 'REST APIs', 'Webhooks', 'PostgreSQL', 'GraphQL'] },
    { label: 'Frontend', items: ['JavaScript', 'TypeScript', 'HTML', 'CSS', 'Vue', 'React'] },
    { label: 'Testing', items: ['RSpec', 'FactoryBot', 'TDD', 'Unit testing', 'Integration testing'] },
    { label: 'Cloud and tooling', items: ['AWS', 'Git', 'Linux', 'GitLab CI/CD', 'Datadog', 'Bugsnag'] },
  ],

  languages: 'Spanish and English (fluent)',
};
