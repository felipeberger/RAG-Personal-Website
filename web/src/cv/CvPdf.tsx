import { cv } from './cv';
import { Document, Page, Text, View, StyleSheet, Font } from '@react-pdf/renderer';

// Register a standard font (Helvetica is built-in)
Font.register({
  family: 'Helvetica',
  fonts: [
    { src: 'Helvetica', fontWeight: 400 },
    { src: 'Helvetica-Bold', fontWeight: 700 },
    { src: 'Helvetica-Oblique', fontStyle: 'italic' },
    { src: 'Helvetica-BoldOblique', fontWeight: 700, fontStyle: 'italic' },
  ],
});

const styles = StyleSheet.create({
  page: {
    padding: 48,
    paddingTop: 60,
    paddingBottom: 60,
    fontFamily: 'Helvetica',
    fontSize: 11,
    lineHeight: 1.5,
    color: '#1a1a1a',
  },
  header: {
    textAlign: 'center',
    marginBottom: 8,
    paddingBottom: 16,
  },
  name: {
    fontSize: 26,
    fontWeight: 700,
    lineHeight: 1.2,
    marginBottom: 8,
  },
  headline: {
    fontSize: 13,
    color: '#555',
    lineHeight: 1.4,
    marginBottom: 4,
  },
  contact: {
    fontSize: 10,
    color: '#666',
    marginTop: 2,
    lineHeight: 1.6,
  },
  contactLink: {
    color: '#2563eb',
    textDecoration: 'none',
  },
  section: {
    marginTop: 6,
  },
  sectionTitle: {
    fontSize: 13,
    fontWeight: 700,
    textTransform: 'uppercase',
    letterSpacing: 1,
    color: '#333',
    borderBottom: '1px solid #ccc',
    paddingBottom: 4,
    marginBottom: 6,
  },
  summary: {
    fontSize: 11,
    lineHeight: 1.6,
    color: '#333',
    marginBottom: 8,
  },
  roleGroup: {
    marginTop: 16,
  },
  roleGroupTitle: {
    fontSize: 12,
    fontWeight: 600,
    color: '#555',
    marginBottom: 4,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  role: {
    marginBottom: 12,
  },
  roleHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 4,
  },
  roleTitleOrg: {
    fontWeight: 700,
    fontSize: 11,
  },
  rolePeriod: {
    fontSize: 10,
    color: '#666',
    fontStyle: 'italic',
  },
  bullets: {
    marginLeft: 16,
    marginTop: 4,
  },
  bullet: {
    fontSize: 10,
    lineHeight: 1.6,
    color: '#333',
    marginBottom: 2,
  },
  educationItem: {
    marginBottom: 8,
  },
  eduTitleOrg: {
    fontWeight: 700,
    fontSize: 11,
  },
  eduPeriod: {
    fontSize: 10,
    color: '#666',
    fontStyle: 'italic',
  },
  certItem: {
    marginBottom: 6,
  },
  certTitleOrg: {
    fontWeight: 700,
    fontSize: 11,
  },
  certPeriod: {
    fontSize: 10,
    color: '#666',
    fontStyle: 'italic',
  },
  skillsList: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 16,
    marginTop: 8,
  },
  skillCategory: {
    width: '45%',
  },
  skillLabel: {
    fontWeight: 700,
    fontSize: 10,
    color: '#555',
    marginBottom: 2,
  },
  skillItems: {
    fontSize: 10,
    color: '#333',
  },
  languages: {
    fontSize: 11,
    color: '#333',
    marginTop: 8,
  },
});

export function CvPdfDocument() {
  return (
    <Document>
      <Page size="A4" style={styles.page}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.name}>{cv.name}</Text>
          <Text style={styles.headline}>{cv.headline}</Text>
          <Text style={styles.contact}>
            {cv.location}
          </Text>
          <Text style={styles.contact}>
            <Text style={styles.contactLink}>{cv.links.github}</Text>
            <Text style={styles.contactLink}> · </Text>
            <Text style={styles.contactLink}>{cv.links.linkedin}</Text>
            <Text style={styles.contactLink}> · </Text>
            <Text style={styles.contactLink}>{cv.links.email}</Text>
          </Text>
        </View>

        {/* Summary */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Summary</Text>
          <Text style={styles.summary}>{cv.summary}</Text>
        </View>

        {/* Experience */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Experience</Text>
          {cv.experience.map((group) => (
            <View key={group.heading} style={styles.roleGroup}>
              <Text style={styles.roleGroupTitle}>{group.heading}</Text>
              {group.roles.map((role) => (
                <View key={`${role.org}-${role.period}`} style={styles.role}>
                  <View style={styles.roleHeader}>
                    <Text style={styles.roleTitleOrg}>
                      {role.title}, {role.org}
                    </Text>
                    <Text style={styles.rolePeriod}>{role.period}</Text>
                  </View>
                  <View style={styles.bullets}>
                    {role.bullets.map((bullet, i) => (
                      <Text key={i} style={styles.bullet}>
                        {'\u2022 '}{bullet}
                      </Text>
                    ))}
                  </View>
                </View>
              ))}
            </View>
          ))}
        </View>

        {/* Education */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Education</Text>
          {cv.education.map((edu) => (
            <View key={edu.title} style={styles.educationItem}>
              <Text style={styles.eduTitleOrg}>
                {edu.title}, {edu.org}
              </Text>
              <Text style={styles.eduPeriod}>{edu.period}</Text>
            </View>
          ))}
        </View>

        {/* Certifications */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Certification</Text>
          {cv.certifications.map((cert) => (
            <View key={cert.title} style={styles.certItem}>
              <Text style={styles.certTitleOrg}>
                {cert.title}, {cert.org}
              </Text>
              <Text style={styles.certPeriod}>{cert.period}</Text>
            </View>
          ))}
        </View>

        {/* Skills */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Skills</Text>
          <View style={styles.skillsList}>
            {cv.skills.map((skill) => (
              <View key={skill.label} style={styles.skillCategory}>
                <Text style={styles.skillLabel}>{skill.label}</Text>
                <Text style={styles.skillItems}>{skill.items.join(', ')}</Text>
              </View>
            ))}
          </View>
        </View>

        {/* Languages */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Languages</Text>
          <Text style={styles.languages}>{cv.languages}</Text>
        </View>
      </Page>
    </Document>
  );
}

/** Generate PDF and return a blob URL for download */
export async function generateCvPdfBlob(): Promise<Blob> {
  const { pdf } = await import('@react-pdf/renderer');
  const blob = await pdf(<CvPdfDocument />).toBlob();
  return blob;
}

/** Trigger browser download of the CV PDF */
export async function downloadCvPdf(): Promise<void> {
  try {
    const blob = await generateCvPdfBlob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'Felipe_Berger_CV.pdf';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch (error) {
    console.error('Failed to generate CV PDF:', error);
    throw new Error('Could not generate PDF. Please try again.');
  }
}