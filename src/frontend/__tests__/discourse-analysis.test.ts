import {
  cleanTranscript,
  runAnalysis,
  type AuditMetadata,
} from "@/lib/discourse-analysis";

const metadata: AuditMetadata = {
  operatorFirstName: "Rayan",
  advisorName: "",
  operatorEmail: "rayan@example.fr",
  speakerMode: "auto",
  callType: "SAV / technique",
  trainings: [],
};

const transcript = `
[00:00:01 → 00:00:04] Speaker 1: Bonjour, je m appelle Rayan, comment puis-je vous aider ?
[00:00:05 → 00:00:10] Speaker 2: Je suis toute seule et je ne sais pas quoi faire avec mon réfrigérateur.
[00:00:11 → 00:00:20] Speaker 1: Je comprends, on va regarder ensemble. Si j ai bien compris, le moteur fait du bruit depuis hier ?
[00:00:21 → 00:00:28] Speaker 1: Je vais vérifier la référence et je vous propose une intervention demain.
[00:00:29 → 00:00:36] Speaker 1: Je vous récapitule la prochaine étape. Est-ce que tout est clair, Madame ? Bonne journée.
`;

describe("discourse analysis", () => {
  it("cleans SRT and VTT markers", () => {
    expect(cleanTranscript("WEBVTT\n\n1\n00:00:01,000 --> 00:00:02,000\nBonjour")).toBe("Bonjour");
  });

  it("analyzes labelled speakers and produces all seven steps", () => {
    const result = runAnalysis([{ name: "appel.txt", content: transcript }], metadata, new Date("2026-07-25T10:00:00Z"));

    expect(result.analyses).toHaveLength(1);
    expect(result.analyses[0].hasLabels).toBe(true);
    expect(result.analyses[0].signals.found).toEqual(expect.arrayContaining(["je suis toute seule", "je ne sais pas"]));
    expect(result.analyses[0].signals.rebounds).toBe(true);
    expect(result.global.steps).toHaveLength(7);
    expect(result.global.labelRate).toBe(100);
    expect(result.report).toContain("RAPPORT V4.1");
    expect(result.mail).toContain("Bonjour Rayan");
  });

  it("consolidates up to five inputs and detects friction", () => {
    const second = `${transcript}\nSpeaker 1: Je viens de vous le dire.`;
    const result = runAnalysis(
      [
        { name: "appel-1.txt", content: transcript },
        { name: "appel-2.txt", content: second },
      ],
      metadata,
    );

    expect(result.analyses).toHaveLength(2);
    expect(result.global.frictions).toBe(1);
    expect(result.global.average).toBeGreaterThanOrEqual(0);
    expect(result.global.average).toBeLessThanOrEqual(100);
  });

  it("rejects an empty analysis", () => {
    expect(() => runAnalysis([{ name: "vide.txt", content: "" }], metadata)).toThrow(
      "Au moins une transcription est requise.",
    );
  });
});
