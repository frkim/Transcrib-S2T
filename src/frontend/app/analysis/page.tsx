import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AppHeader from "@/components/AppHeader";
import AnalysisWorkspace from "@/components/AnalysisWorkspace";

export default async function AnalysisPage({
  searchParams,
}: {
  searchParams: Promise<{ job?: string | string[] }>;
}) {
  const { job } = await searchParams;
  const initialJobIds = Array.isArray(job) ? job : job ? [job] : [];

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppHeader />
      <Container maxWidth="xl" sx={{ py: { xs: 3, md: 4 } }}>
        <Stack spacing={3}>
          <Box sx={{ maxWidth: 880 }}>
            <Typography variant="overline" color="primary.main" sx={{ fontWeight: 800 }}>
              Analyse qualitative V4.1
            </Typography>
            <Typography variant="h1" gutterBottom>
              Qualité du discours client
            </Typography>
            <Typography color="text.secondary" sx={{ fontSize: 17 }}>
              Comparez jusqu’à cinq conversations, mesurez la maîtrise des sept étapes et préparez un plan de coaching étayé par les preuves du transcript.
            </Typography>
          </Box>
          <AnalysisWorkspace initialJobIds={initialJobIds} />
        </Stack>
      </Container>
    </Box>
  );
}