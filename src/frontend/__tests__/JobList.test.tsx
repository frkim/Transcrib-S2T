import { render, screen } from "@testing-library/react";
import JobList from "@/components/JobList";
import type { TranscriptionJob } from "@/lib/types";

function job(overrides: Partial<TranscriptionJob>): TranscriptionJob {
  return {
    id: "1",
    fileName: "sample.mp3",
    audioBlobUrl: "https://example/audio/1.mp3",
    transcriptBlobUrl: null,
    status: "Processing",
    error: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  };
}

describe("JobList", () => {
  it("renders an empty state", () => {
    render(<JobList jobs={[]} />);
    expect(screen.getByText(/no transcription jobs/i)).toBeInTheDocument();
  });

  it("shows a download link only for completed jobs", () => {
    render(
      <JobList
        jobs={[
          job({ id: "1", fileName: "a.mp3", status: "Processing" }),
          job({ id: "2", fileName: "b.mp3", status: "Completed" }),
        ]}
      />
    );

    expect(screen.getByText("a.mp3")).toBeInTheDocument();
    const links = screen.getAllByRole("link", { name: /download/i });
    expect(links).toHaveLength(1);
    expect(links[0]).toHaveAttribute("href", expect.stringContaining("/jobs/2/transcript"));
  });
});
