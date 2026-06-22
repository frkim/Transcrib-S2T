"use client";

import { useEffect, useState } from "react";
import type { TranscriptionJob } from "@/lib/types";
import { listJobs } from "@/lib/api";
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

    // Initial load plus periodic polling so statuses update over time.
    void load();
    const timer = setInterval(() => void load(), 5000);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [reloadToken]);

  return (
    <main style={{ maxWidth: 800, margin: "2rem auto", padding: "0 1rem" }}>
      <h1>Transcrib-S2T</h1>
      <p>Upload one or more MP3 files to generate speech-to-text transcripts.</p>

      <section>
        <h2>Upload</h2>
        <UploadForm onUploaded={() => setReloadToken((t) => t + 1)} />
      </section>

      <section>
        <h2>Jobs</h2>
        {error && <p role="alert">{error}</p>}
        <JobList jobs={jobs} />
      </section>
    </main>
  );
}
