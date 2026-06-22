import type { TranscriptionJob } from "./types";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "") ?? "";

function url(path: string): string {
  return `${API_BASE_URL}${path}`;
}

/** Lists all transcription jobs. */
export async function listJobs(): Promise<TranscriptionJob[]> {
  const response = await fetch(url("/jobs"), { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`Failed to list jobs (${response.status})`);
  }
  return (await response.json()) as TranscriptionJob[];
}

/** Uploads one or more MP3 files to create transcription jobs. */
export async function uploadJobs(files: File[]): Promise<void> {
  const form = new FormData();
  for (const file of files) {
    form.append("files", file, file.name);
  }

  const response = await fetch(url("/jobs"), {
    method: "POST",
    body: form,
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(`Upload failed (${response.status}): ${message}`);
  }
}

/** Builds the transcript download URL for a job. */
export function transcriptUrl(jobId: string): string {
  return url(`/jobs/${jobId}/transcript`);
}
