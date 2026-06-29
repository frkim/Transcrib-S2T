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

/**
 * Processing time in milliseconds between the upload (createdAt) and the moment
 * the transcript became available (updatedAt). For jobs still processing it
 * returns the live elapsed time since upload. Returns null when not applicable.
 */
export function jobDurationMs(
  job: { status: JobStatus; createdAt: string; updatedAt: string },
  now: number = Date.now()
): number | null {
  const start = new Date(job.createdAt).getTime();
  if (Number.isNaN(start)) {
    return null;
  }
  switch (job.status) {
    case "Completed":
    case "Failed": {
      const end = new Date(job.updatedAt).getTime();
      return Number.isNaN(end) ? null : Math.max(0, end - start);
    }
    case "Processing":
      return Math.max(0, now - start);
    default:
      return null;
  }
}

/** Formats a millisecond duration compactly, e.g. "12 s" or "1 min 05 s". */
export function formatDuration(ms: number): string {
  const totalSeconds = Math.round(ms / 1000);
  if (totalSeconds < 60) {
    return `${totalSeconds} s`;
  }
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes} min ${seconds.toString().padStart(2, "0")} s`;
}
