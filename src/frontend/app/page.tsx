"use client";

import { useCallback, useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { TranscriptionJob } from "@/lib/types";
import { deleteJob, listJobs } from "@/lib/api";
import AppHeader from "@/components/AppHeader";
import UploadForm from "@/components/UploadForm";
import JobList from "@/components/JobList";

export default function Home() {
  const [jobs, setJobs] = useState<TranscriptionJob[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const data = await listJobs();
        if (active) {
          setJobs(data);
          setError(null);
        }
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load jobs.");
        }
      }
    }

    // Initial load plus periodic polling so statuses (and live durations)
    // update quickly after an upload.
    void load();
    const timer = setInterval(() => void load(), 2000);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [reloadToken]);

  const handleDelete = useCallback(async (jobId: string) => {
    try {
      await deleteJob(jobId);
      setJobs((current) => current.filter((job) => job.id !== jobId));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete job.");
    }
  }, []);

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppHeader />

      <Container maxWidth="lg" sx={{ py: { xs: 3, md: 4 } }}>
        <Stack spacing={4}>
          <Box>
            <Typography variant="overline" color="primary.main" sx={{ fontWeight: 800 }}>
              Transcription audio
            </Typography>
            <Typography variant="h1" gutterBottom>
              Transformez vos conversations en données exploitables
            </Typography>
            <Typography color="text.secondary" sx={{ fontSize: 17, maxWidth: 760 }}>
              Déposez un ou plusieurs MP3, suivez leur traitement et ouvrez les transcripts terminés dans l’espace d’analyse qualitative.
            </Typography>
          </Box>

          <Paper variant="outlined" sx={{ p: 3 }}>
            <Typography variant="h2" gutterBottom>
              Nouveaux fichiers audio
            </Typography>
            <UploadForm onUploaded={() => setReloadToken((t) => t + 1)} />
          </Paper>

          <Box>
            <Typography variant="h2" gutterBottom>
              Transcriptions
            </Typography>
            {error && (
              <Alert severity="error" role="alert" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}
            <JobList jobs={jobs} onDelete={handleDelete} />
          </Box>
        </Stack>
      </Container>
    </Box>
  );
}
