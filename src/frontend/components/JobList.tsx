import type { TranscriptionJob } from "@/lib/types";
import { canDownloadTranscript, statusLabel } from "@/lib/jobs";
import { transcriptUrl } from "@/lib/api";

interface JobListProps {
  jobs: TranscriptionJob[];
}

export default function JobList({ jobs }: JobListProps) {
  if (jobs.length === 0) {
    return <p>No transcription jobs yet.</p>;
  }

  return (
    <table>
      <thead>
        <tr>
          <th>File</th>
          <th>Status</th>
          <th>Created</th>
          <th>Transcript</th>
        </tr>
      </thead>
      <tbody>
        {jobs.map((job) => (
          <tr key={job.id}>
            <td>{job.fileName}</td>
            <td data-status={job.status}>{statusLabel(job.status)}</td>
            <td>{new Date(job.createdAt).toLocaleString()}</td>
            <td>
              {canDownloadTranscript(job.status) ? (
                <a href={transcriptUrl(job.id)}>Download</a>
              ) : job.status === "Failed" ? (
                <span title={job.error ?? undefined}>—</span>
              ) : (
                "—"
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
