import {
  canDownloadTranscript,
  isMp3File,
  partitionMp3Files,
  statusLabel,
} from "@/lib/jobs";

describe("isMp3File", () => {
  it("accepts .mp3 files with audio/mpeg type", () => {
    expect(isMp3File({ name: "talk.mp3", type: "audio/mpeg" })).toBe(true);
  });

  it("accepts .mp3 files even without a content type", () => {
    expect(isMp3File({ name: "talk.MP3" })).toBe(true);
  });

  it("rejects non-mp3 files", () => {
    expect(isMp3File({ name: "notes.txt", type: "text/plain" })).toBe(false);
    expect(isMp3File({ name: "audio.wav", type: "audio/wav" })).toBe(false);
  });
});

describe("partitionMp3Files", () => {
  it("splits accepted and rejected files", () => {
    const { accepted, rejected } = partitionMp3Files([
      { name: "a.mp3", type: "audio/mpeg" },
      { name: "b.txt", type: "text/plain" },
      { name: "c.mp3" },
    ]);

    expect(accepted.map((f) => f.name)).toEqual(["a.mp3", "c.mp3"]);
    expect(rejected).toEqual(["b.txt"]);
  });
});

describe("status helpers", () => {
  it("only allows download when completed", () => {
    expect(canDownloadTranscript("Completed")).toBe(true);
    expect(canDownloadTranscript("Processing")).toBe(false);
    expect(canDownloadTranscript("Failed")).toBe(false);
    expect(canDownloadTranscript("Purged")).toBe(false);
  });

  it("maps status labels", () => {
    expect(statusLabel("Processing")).toBe("Processing");
    expect(statusLabel("Failed")).toBe("Failed");
  });
});
