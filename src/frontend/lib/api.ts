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

/** Loads one transcription job. */
export async function getJob(jobId: string): Promise<TranscriptionJob> {
  const response = await fetch(url(`/jobs/${jobId}`), { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`Failed to load job (${response.status})`);
  }
  return (await response.json()) as TranscriptionJob;
}

/** Loads the UTF-8 transcript content through the API. */
export async function getTranscript(jobId: string): Promise<string> {
  const response = await fetch(url(`/jobs/${jobId}/transcript`), {
    cache: "no-store",
    headers: { Accept: "text/plain" },
  });
  if (!response.ok) {
    throw new Error(`Failed to load transcript (${response.status})`);
  }
  return response.text();
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

/** Deletes a transcription job and its associated blobs. */
export async function deleteJob(jobId: string): Promise<void> {
  const response = await fetch(url(`/jobs/${jobId}`), { method: "DELETE" });
  if (!response.ok) {
    throw new Error(`Failed to delete job (${response.status})`);
  }
}
