"use client";

import { useRef, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import { partitionMp3Files } from "@/lib/jobs";
import { uploadJobs } from "@/lib/api";

interface UploadFormProps {
  onUploaded: () => void;
}

export default function UploadForm({ onUploaded }: UploadFormProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [fileCount, setFileCount] = useState(0);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setMessage(null);
    setError(null);

    const selected = Array.from(inputRef.current?.files ?? []);
    if (selected.length === 0) {
      setError("Please select at least one MP3 file.");
      return;
    }

    const { accepted, rejected } = partitionMp3Files(selected);
    if (accepted.length === 0) {
      setError("Only .mp3 files are accepted.");
      return;
    }

    setBusy(true);
    try {
      await uploadJobs(accepted as File[]);
      const skipped = rejected.length > 0 ? ` (skipped: ${rejected.join(", ")})` : "";
      setMessage(`Uploaded ${accepted.length} file(s)${skipped}.`);
      if (inputRef.current) {
        inputRef.current.value = "";
      }
      setFileCount(0);
      onUploaded();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Box component="form" onSubmit={handleSubmit} aria-label="upload-form">
      <Stack spacing={2} sx={{ alignItems: "flex-start" }}>
        <Box
          sx={{
            display: "flex",
            flexDirection: "row",
            alignItems: "center",
            flexWrap: "wrap",
            gap: 2,
          }}
        >
          <Button
            component="label"
            variant="outlined"
            startIcon={<CloudUploadIcon />}
            disabled={busy}
          >
            Choose MP3 files
            <input
              ref={inputRef}
              type="file"
              accept="audio/mpeg,.mp3"
              multiple
              hidden
              disabled={busy}
              aria-label="mp3-files"
              onChange={(e) => setFileCount(e.target.files?.length ?? 0)}
            />
          </Button>
          {fileCount > 0 && (
            <Typography variant="body2" color="text.secondary">
              {fileCount} file(s) selected
            </Typography>
          )}
          <Button
            type="submit"
            variant="contained"
            disabled={busy}
            startIcon={
              busy ? <CircularProgress size={18} color="inherit" /> : undefined
            }
          >
            {busy ? "Uploading…" : "Upload"}
          </Button>
        </Box>
        {message && (
          <Alert severity="success" role="status" sx={{ width: "100%" }}>
            {message}
          </Alert>
        )}
        {error && (
          <Alert severity="error" role="alert" sx={{ width: "100%" }}>
            {error}
          </Alert>
        )}
      </Stack>
    </Box>
  );
}
