import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
    expect(screen.getByText(/aucune transcription/i)).toBeInTheDocument();
  });

  it("shows download and analysis links only for completed jobs", () => {
    render(
      <JobList
        jobs={[
          job({ id: "1", fileName: "a.mp3", status: "Processing" }),
          job({ id: "2", fileName: "b.mp3", status: "Completed" }),
        ]}
      />
    );

    expect(screen.getByText("a.mp3")).toBeInTheDocument();
    const downloadLinks = screen.getAllByRole("link", { name: /télécharger/i });
    expect(downloadLinks).toHaveLength(1);
    expect(downloadLinks[0]).toHaveAttribute("href", expect.stringContaining("/jobs/2/transcript"));

    const analysisLinks = screen.getAllByRole("link", { name: /analyser/i });
    expect(analysisLinks).toHaveLength(1);
    expect(analysisLinks[0]).toHaveAttribute("href", "/analysis?job=2");
  });

  it("shows a processing indicator for processing jobs", () => {
    render(<JobList jobs={[job({ id: "1", status: "Processing" })]} />);
    expect(screen.getByLabelText(/processing/i)).toBeInTheDocument();
  });

  it("calls onDelete when the delete button is clicked", async () => {
    const onDelete = jest.fn();
    render(
      <JobList
        jobs={[job({ id: "42", fileName: "talk.mp3" })]}
        onDelete={onDelete}
      />
    );

    await userEvent.click(
      screen.getByRole("button", { name: /supprimer talk.mp3/i })
    );
    expect(onDelete).toHaveBeenCalledWith("42");
  });
});
