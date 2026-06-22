import type { JobStatus } from "./types";

/** Client-side validation: only MP3 files are accepted. */
export function isMp3File(file: { name: string; type?: string }): boolean {
  const hasMp3Extension = file.name.toLowerCase().endsWith(".mp3");
  const typeOk = !file.type || file.type === "audio/mpeg";
  return hasMp3Extension && typeOk;
}

/** Splits a file selection into accepted MP3 files and rejected names. */
export function partitionMp3Files(files: { name: string; type?: string }[]): {
  accepted: { name: string; type?: string }[];
  rejected: string[];
} {
  const accepted: { name: string; type?: string }[] = [];
  const rejected: string[] = [];
  for (const file of files) {
    if (isMp3File(file)) {
      accepted.push(file);
    } else {
      rejected.push(file.name);
    }
  }
  return { accepted, rejected };
}

/** Human-readable label for a job status. */
export function statusLabel(status: JobStatus): string {
  switch (status) {
    case "Processing":
      return "Processing";
    case "Completed":
      return "Completed";
    case "Failed":
      return "Failed";
    case "Purged":
      return "Purged";
    default:
      return status;
  }
}

/** Whether a transcript can be downloaded for the given job status. */
export function canDownloadTranscript(status: JobStatus): boolean {
  return status === "Completed";
}
