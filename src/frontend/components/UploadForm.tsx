"use client";

import { useRef, useState } from "react";
import { partitionMp3Files } from "@/lib/jobs";
import { uploadJobs } from "@/lib/api";

interface UploadFormProps {
  onUploaded: () => void;
}

export default function UploadForm({ onUploaded }: UploadFormProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
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
      onUploaded();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} aria-label="upload-form">
      <input
        ref={inputRef}
        type="file"
        accept="audio/mpeg,.mp3"
        multiple
        disabled={busy}
        aria-label="mp3-files"
      />
      <button type="submit" disabled={busy}>
        {busy ? "Uploading…" : "Upload"}
      </button>
      {message && <p role="status">{message}</p>}
      {error && <p role="alert">{error}</p>}
    </form>
  );
}
