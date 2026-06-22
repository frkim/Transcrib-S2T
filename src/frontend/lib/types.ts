export type JobStatus =
  | "Processing"
  | "Completed"
  | "Failed"
  | "Purged";

export interface TranscriptionJob {
  id: string;
  fileName: string;
  audioBlobUrl: string;
  transcriptBlobUrl: string | null;
  status: JobStatus;
  error: string | null;
  createdAt: string;
  updatedAt: string;
}
