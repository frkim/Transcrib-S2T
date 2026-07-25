export type SpeakerMode = "auto" | "speaker1" | "speaker2" | "labels";

export interface AuditMetadata {
  operatorFirstName: string;
  advisorName: string;
  operatorEmail: string;
  speakerMode: SpeakerMode;
  callType: string;
  trainings: string[];
}

export interface StepDefinition {
  id: string;
  name: string;
  shortName: string;
  weight: number;
  positiveTerms: string[];
  required: string[];
  coachingPhrase: string;
  training: string;
}

export interface StepResult {
  id: string;
  name: string;
  shortName: string;
  score: number;
  level: string;
  evidence: string;
  missing: string;
  coachingPhrase: string;
}

export interface QuestionProfile {
  total: number;
  administrative: number;
  technical: number;
  relational: number;
}

export interface CollectionStatus {
  emailAsked: boolean;
  emailFound: boolean;
  phoneAsked: boolean;
  phoneFound: boolean;
}

export interface ConclusionStatus {
  recap: boolean;
  nextStep: boolean;
  understandingCheck: boolean;
  personalized: boolean;
}

export interface TranscriptAnalysis {
  index: number;
  sourceName: string;
  score: number;
  advisorWords: number;
  totalWords: number;
  hasLabels: boolean;
  reliability: "Forte" | "Moyenne" | "Faible";
  rows: StepResult[];
  advisorText: string;
  clientText: string;
  transcript: string;
  questionProfile: QuestionProfile;
  collection: CollectionStatus;
  signals: { found: string[]; rebounds: boolean };
  frictions: string[];
  obsolete: string[];
  conclusion: ConclusionStatus;
}

export interface GlobalAnalysis {
  average: number;
  labelRate: number;
  steps: StepResult[];
  priority: StepResult;
  strength: StepResult;
  level: string;
  signals: number;
  frictions: number;
  advisorWords: number;
}

export interface AnalysisBundle {
  createdAt: string;
  metadata: AuditMetadata;
  analyses: TranscriptAnalysis[];
  global: GlobalAnalysis;
  mail: string;
  report: string;
}

export interface TranscriptInput {
  name: string;
  content: string;
}

export const CALL_TYPES = [
  "Non précisé",
  "SAV / technique",
  "Livraison",
  "Réclamation",
  "Suivi dossier",
  "Création fiche / prise en charge",
  "Assurance / abonnement",
] as const;

export const TRAININGS = [
  "Accueil / ouverture",
  "Identification / collecte",
  "Écoute active",
  "Reformulation",
  "Questionnement technique",
  "Traitement / résolution",
  "Conclusion",
  "Gestion des signaux sensibles",
] as const;

export const STEPS: StepDefinition[] = [
  {
    id: "opening",
    name: "1. Accueil / ouverture",
    shortName: "Accueil",
    weight: 11,
    positiveTerms: ["bonjour", "service technique", "darty", "fnac", "je m appelle", "comment puis-je vous aider"],
    required: ["présentation personnelle", "accueil de la demande"],
    coachingPhrase: "Bonjour, je vais reprendre votre demande avec vous et voir comment vous aider concrètement.",
    training: "Accueil / ouverture",
  },
  {
    id: "identification",
    name: "2. Identification / collecte",
    shortName: "Identification",
    weight: 13,
    positiveTerms: ["nom", "prénom", "adresse", "code postal", "téléphone", "portable", "mail", "fiche client", "dossier"],
    required: ["collecte fluide", "coordonnées obtenues"],
    coachingPhrase: "Je vais créer votre fiche, je vous explique pourquoi je vous demande ces informations.",
    training: "Identification / collecte",
  },
  {
    id: "active-listening",
    name: "3. Écoute active / besoin",
    shortName: "Écoute active",
    weight: 16,
    positiveTerms: ["je comprends", "je vous rassure", "je vois", "vous me dites", "je prends note", "je vais vous aider"],
    required: ["rebond relationnel", "prise en compte du contexte client"],
    coachingPhrase: "Je comprends que ce soit compliqué, on va faire simple et je vais vous guider étape par étape.",
    training: "Écoute active",
  },
  {
    id: "rephrasing",
    name: "4. Reformulation",
    shortName: "Reformulation",
    weight: 13,
    positiveTerms: ["si j ai bien compris", "vous souhaitez", "je récapitule", "pour résumer", "donc votre demande", "vous me confirmez"],
    required: ["confirmation du symptôme", "validation des informations"],
    coachingPhrase: "Si je comprends bien, votre appareil fait du froid mais les joints chauffent après dégivrage.",
    training: "Reformulation",
  },
  {
    id: "questioning",
    name: "5. Questionnement utile",
    shortName: "Questionnement",
    weight: 14,
    positiveTerms: ["depuis quand", "bruit", "moteur", "compresseur", "température", "voyant", "dégivré", "débrancher", "symptôme", "référence", "numéro de série"],
    required: ["questions techniques", "questions ciblées"],
    coachingPhrase: "Pour poser le bon diagnostic, je vais vérifier avec vous depuis quand le phénomène apparaît et si le moteur tourne.",
    training: "Questionnement technique",
  },
  {
    id: "resolution",
    name: "6. Traitement / résolution",
    shortName: "Résolution",
    weight: 23,
    positiveTerms: ["je vais vérifier", "je vous explique", "solution", "débrancher", "rebrancher", "rappel", "intervention", "rendez-vous", "prochaine étape", "je vous propose"],
    required: ["consigne claire", "suite sécurisée"],
    coachingPhrase: "Voici ce que vous faites maintenant, puis je vous rappelle entre 13h et 14h pour vérifier le résultat.",
    training: "Traitement / résolution",
  },
  {
    id: "closing",
    name: "7. Conclusion / prise de congé",
    shortName: "Conclusion",
    weight: 10,
    positiveTerms: ["bonne journée", "au revoir", "je vous récapitule", "est-ce que tout est clair", "prochaine étape", "je vous rappelle"],
    required: ["récapitulatif", "vérification compréhension", "prise de congé personnalisée"],
    coachingPhrase: "Je vous récapitule : vous débranchez maintenant, vous rebranchez à midi et je vous rappelle entre 13h et 14h.",
    training: "Conclusion",
  },
];

const SENSITIVE_TERMS = [
  "75 ans",
  "je suis seul",
  "je suis toute seule",
  "souffrir du coeur",
  "souffert du coeur",
  "j ai peur",
  "je sais pas quoi faire",
  "pas évident",
  "je ne sais pas",
];

const EMPATHY_TERMS = [
  "je comprends",
  "je vous rassure",
  "on va faire simple",
  "je vais vous guider",
  "ne vous inquiétez pas",
  "on va regarder ensemble",
];

const FRICTION_TERMS = [
  "je viens de vous le dire",
  "je vous l ai dit",
  "je vous l’ai dit",
  "je vous entends très mal",
  "je vous entends mal",
  "vous me rendez beaucoup",
  "je ne sais pas où",
  "j ai rien du tout",
];

const OBSOLETE_TERMS = [
  "enquête svi",
  "question après l appel",
  "répondre à la question après l appel",
];

export function normalizeText(value: string): string {
  return String(value ?? "")
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[’']/g, " ");
}

export function wordCount(value: string): number {
  return normalizeText(value).match(/[a-z0-9€%]+/g)?.length ?? 0;
}

export function cleanTranscript(value: string): string {
  return String(value ?? "")
    .replace(/\d{2}:\d{2}:\d{2}[,.]\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}[,.]\d{3}/g, "")
    .replace(/^\d+$/gm, "")
    .replace(/WEBVTT/g, "")
    .trim();
}

function contains(text: string, phrase: string): boolean {
  const query = normalizeText(phrase);
  return query.length > 0 && normalizeText(text).includes(query);
}

function countTerms(text: string, phrases: string[]): number {
  return phrases.reduce((total, phrase) => total + (contains(text, phrase) ? 1 : 0), 0);
}

function parseSpeakerLine(line: string): { speaker: string; text: string } | null {
  const match = line.match(
    /^(?:\[[^\]]+\]\s*)?(Speaker\s*1|Speaker\s*2|client|conseiller|conseill[èe]re|agent|charg[ée]\s+de\s+client[èe]le|op[ée]rateur|op[ée]ratrice)\s*[:\-–]\s*(.+)$/i,
  );
  return match ? { speaker: normalizeText(match[1]), text: match[2] } : null;
}

function splitSpeakers(text: string, mode: SpeakerMode) {
  const lines = cleanTranscript(text)
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
  const advisor: string[] = [];
  const client: string[] = [];
  const other: string[] = [];
  let hasLabels = false;

  for (const line of lines) {
    const parsed = parseSpeakerLine(line);
    if (!parsed) {
      other.push(line);
      continue;
    }

    hasLabels = true;
    const speaker = parsed.speaker;
    if (mode === "speaker1") {
      (speaker.includes("speaker 1") ? advisor : client).push(parsed.text);
    } else if (mode === "speaker2") {
      (speaker.includes("speaker 2") ? advisor : client).push(parsed.text);
    } else if (speaker.includes("client") || speaker.includes("speaker 2")) {
      client.push(parsed.text);
    } else {
      advisor.push(parsed.text);
    }
  }

  return {
    hasLabels,
    advisorText: advisor.length > 0 ? advisor.join("\n") : cleanTranscript(text),
    clientText: client.join("\n"),
    otherText: other.join("\n"),
  };
}

function findEvidence(text: string, phrases: string[]): string {
  const lines = String(text ?? "")
    .split(/\r?\n|[.!?]+\s+/)
    .map((line) => line.trim())
    .filter(Boolean);
  for (const phrase of phrases) {
    const matchingLine = lines.find((line) => contains(line, phrase));
    if (matchingLine) {
      return matchingLine.length > 220 ? `${matchingLine.slice(0, 217)}...` : matchingLine;
    }
  }
  return "";
}

export function levelForScore(score: number): string {
  if (score >= 85) return "Très bon";
  if (score >= 70) return "Bon";
  if (score >= 55) return "Correct";
  if (score >= 35) return "À renforcer";
  return "Fragile";
}

function questionProfile(text: string): QuestionProfile {
  return {
    total: text.match(/\?/g)?.length ?? 0,
    administrative: countTerms(text, ["nom", "prénom", "adresse", "code postal", "téléphone", "portable", "mail", "numéro de voie"]),
    technical: countTerms(text, ["depuis quand", "bruit", "moteur", "compresseur", "température", "dégivré", "chauffe", "froid", "numéro de série", "référence"]),
    relational: countTerms(text, ["vous me dites", "si j ai bien compris", "vous souhaitez", "je comprends"]),
  };
}

function collectionStatus(full: string, advisor: string): CollectionStatus {
  return {
    emailAsked: countTerms(advisor, ["mail", "email", "adresse mail"]) > 0,
    emailFound: /[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}/i.test(full),
    phoneAsked: countTerms(advisor, ["téléphone", "portable", "fixe"]) > 0,
    phoneFound: /(0\s*[1-9](?:[\s.-]*\d{2}){4})/.test(full),
  };
}

function conclusionStatus(advisor: string): ConclusionStatus {
  return {
    recap: countTerms(advisor, ["je vous récapitule", "donc vous allez", "pour résumer"]) > 0,
    nextStep: countTerms(advisor, ["je vous rappelle", "prochaine étape", "entre 13h et 14h", "demain"]) > 0,
    understandingCheck: countTerms(advisor, ["est-ce que tout est clair", "vous avez compris", "c est bon pour vous"]) > 0,
    personalized: countTerms(advisor, ["madame", "monsieur"]) > 0,
  };
}

function requirementMet(
  requirement: string,
  context: {
    advisor: string;
    questionProfile: QuestionProfile;
    collection: CollectionStatus;
    signals: { found: string[]; rebounds: boolean };
    conclusion: ConclusionStatus;
  },
): boolean {
  const normalized = normalizeText(requirement);
  if (normalized.includes("technique") || normalized.includes("questions ciblees")) return context.questionProfile.technical > 0;
  if (normalized.includes("coordonnees") || normalized.includes("collecte")) return context.collection.phoneFound || context.collection.emailFound;
  if (normalized.includes("rebond") || normalized.includes("contexte client")) return context.signals.found.length === 0 || context.signals.rebounds;
  if (normalized.includes("recap")) return context.conclusion.recap;
  if (normalized.includes("comprehension")) return context.conclusion.understandingCheck;
  if (normalized.includes("suite securisee") || normalized.includes("consigne claire")) return context.conclusion.nextStep || countTerms(context.advisor, ["voici ce que", "vous allez", "je vous propose"]) > 0;
  if (normalized.includes("presentation personnelle")) return countTerms(context.advisor, ["je m appelle"]) > 0;
  if (normalized.includes("accueil de la demande")) return countTerms(context.advisor, ["comment puis-je vous aider", "je vous écoute"]) > 0;
  if (normalized.includes("confirmation") || normalized.includes("validation")) return countTerms(context.advisor, ["si j ai bien compris", "vous me confirmez", "je récapitule"]) > 0;
  if (normalized.includes("prise de conge personnalisee")) return context.conclusion.personalized;
  return true;
}

export function analyzeTranscript(
  input: TranscriptInput,
  index: number,
  speakerMode: SpeakerMode,
): TranscriptAnalysis {
  const speakers = splitSpeakers(input.content, speakerMode);
  const advisor = speakers.advisorText;
  const client = speakers.clientText;
  const full = cleanTranscript(input.content);
  const advisorWords = wordCount(advisor);
  const profile = questionProfile(advisor);
  const collection = collectionStatus(full, advisor);
  const signals = {
    found: SENSITIVE_TERMS.filter((term) => contains(client, term)),
    rebounds: countTerms(advisor, EMPATHY_TERMS) > 0,
  };
  const frictions = FRICTION_TERMS.filter((term) => contains(full, term));
  const obsolete = OBSOLETE_TERMS.filter((term) => contains(advisor, term));
  const conclusion = conclusionStatus(advisor);
  let weightedScore = 0;
  let totalWeight = 0;

  const rows = STEPS.map((step, stepIndex): StepResult => {
    const hits = countTerms(advisor, step.positiveTerms);
    let score = Math.round((hits > 0 ? 48 : 0) + Math.min(30, hits * 8));

    if (stepIndex === 4) {
      score += Math.min(22, profile.technical * 11) + Math.min(8, profile.relational * 4);
      score -= Math.min(14, Math.max(0, profile.administrative - profile.technical) * 3);
    }
    if (stepIndex === 1) {
      if (collection.emailAsked && !collection.emailFound) score -= 12;
      if (collection.phoneAsked && collection.phoneFound) score += 8;
    }
    if (stepIndex === 2 && signals.found.length > 0 && !signals.rebounds) score -= 20;
    if (stepIndex === 6) {
      if (conclusion.recap) score += 15;
      if (conclusion.nextStep) score += 10;
      if (conclusion.understandingCheck) score += 10;
      if (!conclusion.recap) score -= 12;
    }
    if (frictions.length > 0) score -= Math.min(15, frictions.length * 5);

    score = Math.max(0, Math.min(100, score));
    if (advisorWords < 35) score = Math.min(score, 35);
    weightedScore += score * step.weight;
    totalWeight += step.weight;

    const missing = step.required.filter(
      (requirement) =>
        !requirementMet(requirement, {
          advisor,
          questionProfile: profile,
          collection,
          signals,
          conclusion,
        }),
    );

    return {
      id: step.id,
      name: step.name,
      shortName: step.shortName,
      score,
      level: levelForScore(score),
      evidence: findEvidence(advisor, step.positiveTerms),
      missing: missing.join(", ") || "Aucun manque majeur détecté",
      coachingPhrase: step.coachingPhrase,
    };
  });

  return {
    index: index + 1,
    sourceName: input.name,
    score: Math.round(weightedScore / totalWeight),
    advisorWords,
    totalWords: wordCount(input.content),
    hasLabels: speakers.hasLabels,
    reliability: speakers.hasLabels ? (advisorWords >= 80 ? "Forte" : "Moyenne") : advisorWords >= 80 ? "Moyenne" : "Faible",
    rows,
    advisorText: advisor,
    clientText: client,
    transcript: full,
    questionProfile: profile,
    collection,
    signals,
    frictions,
    obsolete,
    conclusion,
  };
}

export function aggregateAnalyses(analyses: TranscriptAnalysis[]): GlobalAnalysis {
  if (analyses.length === 0) {
    throw new Error("Au moins une transcription est requise.");
  }

  const steps = STEPS.map((definition, index): StepResult => {
    const rows = analyses.map((analysis) => analysis.rows[index]);
    return {
      id: definition.id,
      name: definition.name,
      shortName: definition.shortName,
      score: Math.round(rows.reduce((sum, row) => sum + row.score, 0) / rows.length),
      level: levelForScore(Math.round(rows.reduce((sum, row) => sum + row.score, 0) / rows.length)),
      evidence: rows.find((row) => row.evidence)?.evidence ?? "",
      missing: rows.find((row) => row.missing !== "Aucun manque majeur détecté")?.missing ?? "Aucun manque majeur détecté",
      coachingPhrase: definition.coachingPhrase,
    };
  });
  const sortedAscending = [...steps].sort((left, right) => left.score - right.score);
  const sortedDescending = [...steps].sort((left, right) => right.score - left.score);
  const average = Math.round(analyses.reduce((sum, analysis) => sum + analysis.score, 0) / analyses.length);

  return {
    average,
    labelRate: Math.round((analyses.filter((analysis) => analysis.hasLabels).length / analyses.length) * 100),
    steps,
    priority: sortedAscending[0],
    strength: sortedDescending[0],
    level: levelForScore(average),
    signals: analyses.reduce((sum, analysis) => sum + analysis.signals.found.length, 0),
    frictions: analyses.reduce((sum, analysis) => sum + analysis.frictions.length, 0),
    advisorWords: analyses.reduce((sum, analysis) => sum + analysis.advisorWords, 0),
  };
}

function operatorFirstName(metadata: AuditMetadata): string {
  if (metadata.operatorFirstName.trim()) return metadata.operatorFirstName.trim();
  if (metadata.advisorName.trim()) return metadata.advisorName.trim().split(/[\s._-]+/)[0];
  return "[Prénom]";
}

export function buildMail(metadata: AuditMetadata, global: GlobalAnalysis): string {
  const firstName = operatorFirstName(metadata);
  const lines = [
    "Objet : Retour sur ton écoute qualité et plan de progression",
    "",
    `Bonjour ${firstName},`,
    "",
    `Je te partage un retour synthétique sur ton discours client. Ton niveau ressort comme ${global.level}. Ce score est un repère de progression, pas une sanction.`,
    "",
    `Ton point d’appui principal est : ${global.strength.name}. Continue à t’appuyer dessus pour sécuriser l’échange.`,
    "",
    `L’étape prioritaire à travailler est : ${global.priority.name}. Cette étape est importante car elle peut améliorer la clarté pour le client et la fluidité de l’appel.`,
    "",
    "Plan de progression :",
    `- Mois 1 : travailler cette étape avec une phrase repère : « ${global.priority.coachingPhrase} »`,
    "- Mois 2 : appliquer ce réflexe sur des appels réels et débriefer les exemples concrets.",
    "- Mois 3 : refaire une écoute comparative pour mesurer les progrès et stabiliser les bonnes pratiques.",
  ];
  if (global.signals > 0 || global.frictions > 0) {
    lines.push("", "Point d’attention : certains signaux relationnels ou frictions montrent l’importance de mieux reformuler, rassurer et récapituler.");
  }
  lines.push("", "Merci pour ton implication, on avance étape par étape.", "", "À bientôt,");
  return lines.join("\n");
}

function yes(value: boolean): string {
  return value ? "oui" : "non";
}

export function buildReport(
  metadata: AuditMetadata,
  analyses: TranscriptAnalysis[],
  global: GlobalAnalysis,
  createdAt = new Date(),
): string {
  const lines = [
    "RAPPORT V4.1 - ANALYSE QUALITÉ CONSOLIDÉE",
    `Date : ${createdAt.toLocaleString("fr-FR")}`,
    `Opérateur : ${operatorFirstName(metadata)}`,
    `Type appel : ${metadata.callType}`,
    `Score indicatif : ${global.average}/100`,
    `Niveau qualitatif : ${global.level}`,
    `Force principale : ${global.strength.name}`,
    `Étape prioritaire : ${global.priority.name}`,
    `Signaux sensibles : ${global.signals}`,
    `Frictions : ${global.frictions}`,
    `Locuteurs détectés : ${global.labelRate}%`,
    "",
    "7 ÉTAPES",
  ];
  for (const step of global.steps) {
    lines.push(`- ${step.name} : ${step.level} (${step.score}/100)`);
    lines.push(`  Preuve : ${step.evidence || "Aucune preuve explicite détectée."}`);
    lines.push(`  Manque : ${step.missing}`);
    lines.push(`  Phrase à tester : ${step.coachingPhrase}`);
  }
  lines.push("", "DIAGNOSTICS TRANSVERSES");
  for (const analysis of analyses) {
    lines.push(
      `${analysis.sourceName} : signaux ${analysis.signals.found.join(", ") || "aucun"} ; frictions ${analysis.frictions.join(", ") || "aucune"} ; email demandé ${yes(analysis.collection.emailAsked)} / obtenu ${yes(analysis.collection.emailFound)} ; conclusion récap ${yes(analysis.conclusion.recap)}, prochaine étape ${yes(analysis.conclusion.nextStep)}, compréhension ${yes(analysis.conclusion.understandingCheck)}`,
    );
  }
  lines.push(
    "",
    "COACHING 90 JOURS",
    `Mois 1 : sécuriser ${global.priority.name}.`,
    "Mois 2 : mise en pratique et réduction des frictions.",
    "Mois 3 : écoute comparative et validation des progrès.",
    "",
    "MAIL OPÉRATEUR",
    buildMail(metadata, global),
  );
  return lines.join("\n");
}

export function runAnalysis(
  inputs: TranscriptInput[],
  metadata: AuditMetadata,
  now = new Date(),
): AnalysisBundle {
  const analyses = inputs
    .filter((input) => input.content.trim())
    .slice(0, 5)
    .map((input, index) => analyzeTranscript(input, index, metadata.speakerMode));
  const global = aggregateAnalyses(analyses);
  return {
    createdAt: now.toISOString(),
    metadata,
    analyses,
    global,
    mail: buildMail(metadata, global),
    report: buildReport(metadata, analyses, global, now),
  };
}

export function analysisCsv(global: GlobalAnalysis): string {
  const rows = [["etape", "niveau", "score", "preuve", "manque", "phrase"]];
  for (const step of global.steps) {
    rows.push([step.name, step.level, String(step.score), step.evidence, step.missing, step.coachingPhrase]);
  }
  return rows
    .map((row) => row.map((value) => `"${String(value ?? "").replace(/"/g, '""')}"`).join(";"))
    .join("\n");
}
