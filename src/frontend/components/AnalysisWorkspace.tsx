"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import Alert from "@mui/material/Alert";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import FormControl from "@mui/material/FormControl";
import FormControlLabel from "@mui/material/FormControlLabel";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Select from "@mui/material/Select";
import Snackbar from "@mui/material/Snackbar";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import AnalyticsOutlinedIcon from "@mui/icons-material/AnalyticsOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import UploadFileOutlinedIcon from "@mui/icons-material/UploadFileOutlined";
import type { TranscriptionJob } from "@/lib/types";
import { getTranscript, listJobs } from "@/lib/api";
import {
  analysisCsv,
  CALL_TYPES,
  cleanTranscript,
  runAnalysis,
  STEPS,
  TRAININGS,
  wordCount,
  type AnalysisBundle,
  type AuditMetadata,
  type SpeakerMode,
  type TranscriptInput,
} from "@/lib/discourse-analysis";
import AnalysisChart from "@/components/AnalysisChart";

const MAX_TRANSCRIPTS = 5;
const defaultMetadata: AuditMetadata = {
  operatorFirstName: "",
  advisorName: "",
  operatorEmail: "",
  speakerMode: "auto",
  callType: CALL_TYPES[0],
  trainings: [],
};

function downloadFile(name: string, mimeType: string, content: string) {
  const blob = new Blob([content], { type: mimeType });
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = name;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(objectUrl);
}

function levelColor(level: string): "success" | "warning" | "error" | "default" {
  if (level === "Très bon" || level === "Bon") return "success";
  if (level === "Correct") return "warning";
  if (level === "À renforcer" || level === "Fragile") return "error";
  return "default";
}

function Kpi({ label, value, tone }: { label: string; value: string | number; tone?: string }) {
  return (
    <Box sx={{ px: 2, py: 1.75, borderLeft: 3, borderColor: tone ?? "primary.main", bgcolor: "background.paper" }}>
      <Typography variant="caption" color="text.secondary" sx={{ textTransform: "uppercase", fontWeight: 700 }}>
        {label}
      </Typography>
      <Typography variant="h5" sx={{ mt: 0.25, fontWeight: 800 }}>
        {value}
      </Typography>
    </Box>
  );
}

export default function AnalysisWorkspace({ initialJobIds = [] }: { initialJobIds?: string[] }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [jobs, setJobs] = useState<TranscriptionJob[]>([]);
  const [selectedJobs, setSelectedJobs] = useState<TranscriptionJob[]>([]);
  const [inputs, setInputs] = useState<TranscriptInput[]>([]);
  const [activeInput, setActiveInput] = useState(0);
  const [metadata, setMetadata] = useState<AuditMetadata>(defaultMetadata);
  const [result, setResult] = useState<AnalysisBundle | null>(null);
  const [resultTab, setResultTab] = useState(0);
  const [loadingJobs, setLoadingJobs] = useState(true);
  const [loadingTranscripts, setLoadingTranscripts] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const completedJobs = useMemo(
    () => jobs.filter((job) => job.status === "Completed" && job.transcriptBlobUrl),
    [jobs],
  );

  useEffect(() => {
    let active = true;
    listJobs()
      .then((loadedJobs) => {
        if (!active) return;
        setJobs(loadedJobs);
        const selected = loadedJobs.filter(
          (job) => initialJobIds.includes(job.id) && job.status === "Completed" && job.transcriptBlobUrl,
        );
        setSelectedJobs(selected.slice(0, MAX_TRANSCRIPTS));
        setError(null);
      })
      .catch((reason) => {
        if (active) setError(reason instanceof Error ? reason.message : "Impossible de charger les transcriptions.");
      })
      .finally(() => {
        if (active) setLoadingJobs(false);
      });
    return () => {
      active = false;
    };
  }, [initialJobIds]);

  async function loadSelectedJobs() {
    if (selectedJobs.length === 0) {
      setError("Sélectionnez au moins une transcription terminée.");
      return;
    }
    setLoadingTranscripts(true);
    setError(null);
    try {
      const loaded = await Promise.all(
        selectedJobs.map(async (job) => ({
          name: job.fileName.replace(/\.mp3$/i, ".txt"),
          content: cleanTranscript(await getTranscript(job.id)),
        })),
      );
      setInputs(loaded);
      setActiveInput(0);
      setResult(null);
      setToast(`${loaded.length} transcription(s) Azure chargée(s).`);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Impossible de charger les transcripts.");
    } finally {
      setLoadingTranscripts(false);
    }
  }

  async function importFiles(files: FileList | null) {
    const compatible = Array.from(files ?? []).filter(
      (file) => /\.(txt|srt|vtt)$/i.test(file.name) || /^text\//i.test(file.type) || !file.type,
    );
    const availableSlots = MAX_TRANSCRIPTS - inputs.length;
    if (availableSlots <= 0) {
      setError("Cinq transcriptions sont déjà chargées.");
      return;
    }
    if (compatible.length === 0) {
      setError("Sélectionnez un fichier TXT, SRT ou VTT.");
      return;
    }
    const imported = await Promise.all(
      compatible.slice(0, availableSlots).map(async (file) => ({ name: file.name, content: cleanTranscript(await file.text()) })),
    );
    setInputs((current) => [...current, ...imported]);
    setActiveInput(inputs.length);
    setResult(null);
    setError(null);
    setToast(`${imported.length} fichier(s) importé(s).`);
  }

  function addManualTranscript() {
    if (inputs.length >= MAX_TRANSCRIPTS) return;
    setInputs((current) => [...current, { name: `Texte ${current.length + 1}`, content: "" }]);
    setActiveInput(inputs.length);
    setResult(null);
  }

  function updateActiveInput(content: string) {
    setInputs((current) => current.map((input, index) => (index === activeInput ? { ...input, content } : input)));
    setResult(null);
  }

  function removeInput(index: number) {
    setInputs((current) => current.filter((_, currentIndex) => currentIndex !== index));
    setActiveInput((current) => Math.max(0, Math.min(current, inputs.length - 2)));
    setResult(null);
  }

  function analyze(allInputs = inputs) {
    const nonEmpty = allInputs.filter((input) => input.content.trim());
    if (nonEmpty.length === 0) {
      setError("Ajoutez au moins une transcription non vide.");
      return;
    }
    try {
      const bundle = runAnalysis(nonEmpty, metadata);
      setResult(bundle);
      setResultTab(0);
      setError(null);
      setToast("Analyse qualitative terminée.");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "L’analyse a échoué.");
    }
  }

  function updateMetadata(nextMetadata: AuditMetadata) {
    setMetadata(nextMetadata);
    const nonEmpty = inputs.filter((input) => input.content.trim());
    if (result && nonEmpty.length > 0) {
      setResult(runAnalysis(nonEmpty, nextMetadata));
    }
  }

  function analyzeActive() {
    const input = inputs[activeInput];
    if (!input?.content.trim()) {
      setError("Le texte actif est vide.");
      return;
    }
    analyze([input]);
  }

  async function copyText(content: string, successMessage: string) {
    try {
      await navigator.clipboard.writeText(content);
      setToast(successMessage);
    } catch {
      setError("La copie dans le presse-papiers a échoué.");
    }
  }

  function exportHtml() {
    if (!result) return;
    const escaped = result.report.replace(/[&<>"']/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" })[character] ?? character);
    downloadFile(
      "rapport_analyse_qualite.html",
      "text/html;charset=utf-8",
      `<!doctype html><html lang="fr"><meta charset="utf-8"><title>Rapport qualité</title><body style="font-family:Arial,sans-serif;margin:32px;line-height:1.5"><pre style="white-space:pre-wrap">${escaped}</pre></body></html>`,
    );
  }

  function exportMail() {
    if (!result) return;
    const content = `To: ${metadata.operatorEmail.trim()}\r\nSubject: Retour sur ton écoute qualité et coaching\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n${result.mail.replace(/\n/g, "\r\n")}`;
    downloadFile("mail_operateur.eml", "message/rfc822;charset=utf-8", content);
  }

  const activeTranscript = inputs[activeInput];
  const priorityTrainingReceived = result
    ? metadata.trainings.includes(STEPS.find((step) => step.id === result.global.priority.id)?.training ?? "")
    : false;

  return (
    <Stack spacing={3}>
      <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
        <Stack spacing={2.5}>
          <Box>
            <Typography variant="h2">Sources d’analyse</Typography>
            <Typography color="text.secondary" sx={{ mt: 0.5 }}>
              Sélectionnez jusqu’à cinq transcriptions générées dans Azure, ou importez des fichiers TXT, SRT et VTT.
            </Typography>
          </Box>

          <Autocomplete
            multiple
            loading={loadingJobs}
            options={completedJobs}
            value={selectedJobs}
            getOptionLabel={(job) => job.fileName}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            getOptionDisabled={(option) => selectedJobs.length >= MAX_TRANSCRIPTS && !selectedJobs.some((job) => job.id === option.id)}
            onChange={(_, value) => setSelectedJobs(value.slice(0, MAX_TRANSCRIPTS))}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Transcriptions Azure disponibles"
                placeholder="Rechercher un fichier terminé"
                helperText={`${completedJobs.length} transcript(s) disponible(s) dans le compte de stockage`}
              />
            )}
          />

          <Stack direction={{ xs: "column", sm: "row" }} spacing={1.25} useFlexGap sx={{ flexWrap: "wrap" }}>
            <Button
              variant="contained"
              startIcon={loadingTranscripts ? <CircularProgress size={18} color="inherit" /> : <DownloadOutlinedIcon />}
              disabled={loadingTranscripts || selectedJobs.length === 0}
              onClick={loadSelectedJobs}
            >
              Charger la sélection ({selectedJobs.length})
            </Button>
            <Button component="label" variant="outlined" startIcon={<UploadFileOutlinedIcon />}>
              Importer TXT / SRT / VTT
              <input
                ref={inputRef}
                hidden
                multiple
                type="file"
                accept=".txt,.srt,.vtt,text/plain,text/vtt"
                onChange={(event) => {
                  void importFiles(event.target.files);
                  event.target.value = "";
                }}
              />
            </Button>
            <Button variant="text" onClick={addManualTranscript} disabled={inputs.length >= MAX_TRANSCRIPTS}>
              Ajouter un texte manuel
            </Button>
            <Button
              variant="text"
              color="error"
              startIcon={<DeleteOutlineIcon />}
              disabled={inputs.length === 0}
              onClick={() => {
                setInputs([]);
                setSelectedJobs([]);
                setResult(null);
              }}
            >
              Tout vider
            </Button>
          </Stack>

          <Box
            onDragEnter={(event) => {
              event.preventDefault();
              setDragging(true);
            }}
            onDragOver={(event) => event.preventDefault()}
            onDragLeave={() => setDragging(false)}
            onDrop={(event) => {
              event.preventDefault();
              setDragging(false);
              void importFiles(event.dataTransfer.files);
            }}
            sx={{
              p: 2,
              textAlign: "center",
              border: 1,
              borderStyle: "dashed",
              borderColor: dragging ? "primary.main" : "divider",
              bgcolor: dragging ? "rgba(180, 35, 44, 0.06)" : "action.hover",
              borderRadius: 1,
              transition: "background-color 160ms ease, border-color 160ms ease",
            }}
          >
            <Typography variant="body2" sx={{ fontWeight: 700 }}>
              Déposez ici vos fichiers TXT, SRT ou VTT
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Les fichiers sont ajoutés aux emplacements encore disponibles.
            </Typography>
          </Box>
        </Stack>
      </Paper>

      {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}

      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", lg: "360px minmax(0, 1fr)" }, gap: 3, alignItems: "start" }}>
        <Stack spacing={3}>
          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Typography variant="h2" gutterBottom>Informations d’audit</Typography>
            <Stack spacing={2}>
              <TextField label="Prénom opérateur" value={metadata.operatorFirstName} onChange={(event) => updateMetadata({ ...metadata, operatorFirstName: event.target.value })} />
              <TextField label="Nom / identifiant" value={metadata.advisorName} onChange={(event) => updateMetadata({ ...metadata, advisorName: event.target.value })} />
              <TextField label="Mail opérateur" type="email" value={metadata.operatorEmail} onChange={(event) => updateMetadata({ ...metadata, operatorEmail: event.target.value })} />
              <FormControl fullWidth>
                <InputLabel id="speaker-mode-label">Locuteur conseiller</InputLabel>
                <Select
                  labelId="speaker-mode-label"
                  label="Locuteur conseiller"
                  value={metadata.speakerMode}
                  onChange={(event) => updateMetadata({ ...metadata, speakerMode: event.target.value as SpeakerMode })}
                >
                  <MenuItem value="auto">Automatique</MenuItem>
                  <MenuItem value="speaker1">Speaker 1</MenuItem>
                  <MenuItem value="speaker2">Speaker 2</MenuItem>
                  <MenuItem value="labels">Conseiller / Agent / Opérateur</MenuItem>
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel id="call-type-label">Type d’appel</InputLabel>
                <Select
                  labelId="call-type-label"
                  label="Type d’appel"
                  value={metadata.callType}
                  onChange={(event) => updateMetadata({ ...metadata, callType: event.target.value })}
                >
                  {CALL_TYPES.map((callType) => <MenuItem key={callType} value={callType}>{callType}</MenuItem>)}
                </Select>
              </FormControl>
            </Stack>

            <Divider sx={{ my: 2.5 }} />
            <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>Formations déjà reçues</Typography>
            <Box sx={{ mt: 1, display: "grid", gridTemplateColumns: "1fr 1fr" }}>
              {TRAININGS.map((training) => (
                <FormControlLabel
                  key={training}
                  control={
                    <Checkbox
                      size="small"
                      checked={metadata.trainings.includes(training)}
                      onChange={(event) =>
                        updateMetadata({
                          ...metadata,
                          trainings: event.target.checked
                            ? [...metadata.trainings, training]
                            : metadata.trainings.filter((item) => item !== training),
                        })
                      }
                    />
                  }
                  label={<Typography variant="caption">{training}</Typography>}
                />
              ))}
            </Box>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Typography variant="h2" gutterBottom>Exports</Typography>
            <Stack spacing={1}>
              <Button disabled={!result} variant="outlined" onClick={() => result && downloadFile("rapport_analyse_qualite.txt", "text/plain;charset=utf-8", result.report)}>Rapport TXT</Button>
              <Button disabled={!result} variant="outlined" onClick={() => result && downloadFile("analyse_qualite.json", "application/json;charset=utf-8", JSON.stringify(result, null, 2))}>Données JSON</Button>
              <Button disabled={!result} variant="outlined" onClick={() => result && downloadFile("analyse_qualite.csv", "text/csv;charset=utf-8", analysisCsv(result.global))}>Tableau CSV</Button>
              <Button disabled={!result} variant="outlined" onClick={exportHtml}>Rapport HTML</Button>
              <Button disabled={!result} variant="outlined" onClick={exportMail}>Mail .eml</Button>
              <Button disabled={!result} variant="text" startIcon={<ContentCopyOutlinedIcon />} onClick={() => result && void copyText(result.report, "Rapport copié.")}>Copier le rapport</Button>
              <Button disabled={!result} variant="text" startIcon={<ContentCopyOutlinedIcon />} onClick={() => result && void copyText(result.mail, "Mail copié.")}>Copier le mail</Button>
            </Stack>
          </Paper>
        </Stack>

        <Stack spacing={3} sx={{ minWidth: 0 }}>
          <Paper variant="outlined" sx={{ overflow: "hidden" }}>
            {inputs.length > 0 ? (
              <>
                <Tabs value={Math.min(activeInput, inputs.length - 1)} onChange={(_, value) => setActiveInput(value)} variant="scrollable" scrollButtons="auto" aria-label="Transcriptions chargées">
                  {inputs.map((input, index) => <Tab key={`${input.name}-${index}`} label={`${input.name} · ${wordCount(input.content)} mots`} />)}
                </Tabs>
                <Box sx={{ p: { xs: 2, md: 3 } }}>
                  <Stack direction="row" sx={{ mb: 1.5, justifyContent: "space-between", alignItems: "center" }}>
                    <Box>
                      <Typography variant="h2">{activeTranscript?.name}</Typography>
                      <Typography variant="caption" color="text.secondary">Le contenu reste dans votre navigateur pendant l’analyse.</Typography>
                    </Box>
                    <Button color="error" size="small" startIcon={<DeleteOutlineIcon />} onClick={() => removeInput(activeInput)}>Retirer</Button>
                  </Stack>
                  <TextField
                    fullWidth
                    multiline
                    minRows={14}
                    maxRows={24}
                    value={activeTranscript?.content ?? ""}
                    onChange={(event) => updateActiveInput(event.target.value)}
                    placeholder="Collez ici une transcription avec Speaker 1 / Speaker 2, Client / Conseiller…"
                    slotProps={{ htmlInput: { "aria-label": "Contenu de la transcription active" } }}
                  />
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={1.25} sx={{ mt: 2 }}>
                    <Button variant="outlined" startIcon={<AnalyticsOutlinedIcon />} onClick={analyzeActive}>Analyser le texte actif</Button>
                    <Button variant="contained" startIcon={<AnalyticsOutlinedIcon />} onClick={() => analyze()}>Analyse consolidée ({inputs.filter((input) => input.content.trim()).length})</Button>
                  </Stack>
                </Box>
              </>
            ) : (
              <Box sx={{ p: 5, textAlign: "center" }}>
                <AnalyticsOutlinedIcon color="disabled" sx={{ fontSize: 48 }} />
                <Typography variant="h2" sx={{ mt: 1 }}>Aucune transcription chargée</Typography>
                <Typography color="text.secondary" sx={{ mt: 0.75 }}>Sélectionnez des fichiers générés dans Azure ou importez des fichiers locaux.</Typography>
              </Box>
            )}
          </Paper>

          {result && (
            <>
              <Paper variant="outlined" sx={{ p: 2 }}>
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr 1fr", md: "repeat(6, 1fr)" }, gap: 1 }}>
                  <Kpi label="Score" value={`${result.global.average}/100`} tone="primary.main" />
                  <Kpi label="Niveau" value={result.global.level} tone="secondary.main" />
                  <Kpi label="Priorité" value={result.global.priority.shortName} tone="warning.main" />
                  <Kpi label="Signaux" value={result.global.signals} tone="error.main" />
                  <Kpi label="Frictions" value={result.global.frictions} tone="error.dark" />
                  <Kpi label="Locuteurs" value={`${result.global.labelRate}%`} tone="info.main" />
                </Box>
              </Paper>

              <Paper variant="outlined" sx={{ overflow: "hidden" }}>
                <Tabs value={resultTab} onChange={(_, value) => setResultTab(value)} variant="scrollable" scrollButtons="auto" aria-label="Résultats de l’analyse">
                  <Tab label="Synthèse" />
                  <Tab label="Graphique" />
                  <Tab label="7 étapes" />
                  <Tab label="Signaux & frictions" />
                  <Tab label="Coaching" />
                  <Tab label="Mail opérateur" />
                  <Tab label="Rapport" />
                </Tabs>
                <Divider />
                <Box sx={{ p: { xs: 2, md: 3 } }}>
                  {resultTab === 0 && (
                    <Stack spacing={2}>
                      <Alert severity={result.global.average >= 70 ? "success" : result.global.average >= 55 ? "warning" : "error"}>
                        Le discours présente un niveau <strong>{result.global.level}</strong>. L’étape la mieux maîtrisée est <strong>{result.global.strength.name}</strong>. L’étape prioritaire est <strong>{result.global.priority.name}</strong>.
                      </Alert>
                      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}><Typography variant="overline" color="success.main">Force principale</Typography><Typography sx={{ fontWeight: 800 }}>{result.global.strength.name} · {result.global.strength.score}/100</Typography></Paper>
                        <Paper variant="outlined" sx={{ p: 2, flex: 1 }}><Typography variant="overline" color="warning.main">Étape prioritaire</Typography><Typography sx={{ fontWeight: 800 }}>{result.global.priority.name} · {result.global.priority.score}/100</Typography></Paper>
                      </Stack>
                      <Alert severity={result.global.labelRate === 100 ? "success" : result.global.labelRate > 0 ? "warning" : "error"}>
                        Fiabilité {result.global.labelRate === 100 ? "forte" : result.global.labelRate > 0 ? "moyenne" : "faible"} : {result.analyses.filter((analysis) => analysis.hasLabels).length}/{result.analyses.length} transcription(s) avec locuteurs détectés.
                      </Alert>
                    </Stack>
                  )}

                  {resultTab === 1 && <AnalysisChart steps={result.global.steps} />}

                  {resultTab === 2 && (
                    <TableContainer>
                      <Table size="small" aria-label="Évaluation des sept étapes">
                        <TableHead><TableRow><TableCell>Étape</TableCell><TableCell>Niveau</TableCell><TableCell>Score</TableCell><TableCell>Preuve</TableCell><TableCell>Manque détecté</TableCell><TableCell>Phrase à tester</TableCell></TableRow></TableHead>
                        <TableBody>
                          {result.global.steps.map((step) => (
                            <TableRow key={step.id}>
                              <TableCell><strong>{step.name}</strong></TableCell>
                              <TableCell><Chip size="small" color={levelColor(step.level)} label={step.level} /></TableCell>
                              <TableCell>{step.score}/100</TableCell>
                              <TableCell>{step.evidence || <Typography variant="caption" color="text.secondary">Aucune preuve explicite.</Typography>}</TableCell>
                              <TableCell>{step.missing}</TableCell>
                              <TableCell><Typography variant="body2" color="primary.dark">{step.coachingPhrase}</Typography></TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  )}

                  {resultTab === 3 && (
                    <Stack spacing={2}>
                      {result.analyses.map((analysis) => (
                        <Paper key={analysis.sourceName} variant="outlined" sx={{ p: 2 }}>
                          <Typography sx={{ fontWeight: 800 }}>{analysis.sourceName}</Typography>
                          <Typography variant="body2" sx={{ mt: 1 }}>Signaux sensibles : {analysis.signals.found.join(", ") || "aucun"}</Typography>
                          <Typography variant="body2">Rebond relationnel : {analysis.signals.found.length === 0 ? "non applicable" : analysis.signals.rebounds ? "détecté" : "à renforcer"}</Typography>
                          <Typography variant="body2">Frictions : {analysis.frictions.join(", ") || "aucune"}</Typography>
                          <Typography variant="body2">Collecte : email demandé {analysis.collection.emailAsked ? "oui" : "non"} / obtenu {analysis.collection.emailFound ? "oui" : "non"}; téléphone demandé {analysis.collection.phoneAsked ? "oui" : "non"} / obtenu {analysis.collection.phoneFound ? "oui" : "non"}</Typography>
                          <Typography variant="body2">Conclusion : récapitulatif {analysis.conclusion.recap ? "oui" : "non"}, prochaine étape {analysis.conclusion.nextStep ? "oui" : "non"}, compréhension {analysis.conclusion.understandingCheck ? "oui" : "non"}</Typography>
                          <Typography variant="body2">Formulation obsolète : {analysis.obsolete.join(", ") || "aucune"}</Typography>
                        </Paper>
                      ))}
                      <Alert severity="info">Mise en attente : non applicable par défaut. Elle est exclue du score lorsqu’aucune mise en attente explicite n’est détectée.</Alert>
                    </Stack>
                  )}

                  {resultTab === 4 && (
                    <Stack spacing={2}>
                      <Alert severity="warning">
                        <strong>Priorité coaching : {result.global.priority.name}</strong><br />
                        Force d’appui : {result.global.strength.name}. {priorityTrainingReceived ? "Formation déjà reçue : privilégier la pratique et un feedback précis." : "Prévoir une reprise courte puis une mise en situation."}
                      </Alert>
                      {[
                        ["Mois 1 — sécuriser l’étape prioritaire", `Travailler ${result.global.priority.name} avec deux écoutes ciblées. Phrase repère : « ${result.global.priority.coachingPhrase} »`],
                        ["Mois 2 — mise en pratique", "Appliquer ce réflexe sur des appels réels et vérifier la réduction des frictions."],
                        ["Mois 3 — validation", "Réaliser une écoute comparative, mesurer les progrès et stabiliser la bonne pratique."],
                      ].map(([title, content]) => <Paper key={title} variant="outlined" sx={{ p: 2 }}><Typography sx={{ fontWeight: 800 }}>{title}</Typography><Typography color="text.secondary" sx={{ mt: 0.5 }}>{content}</Typography></Paper>)}
                    </Stack>
                  )}

                  {resultTab === 5 && <Box component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "inherit", m: 0 }}>{result.mail}</Box>}
                  {resultTab === 6 && <Box component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "var(--font-geist-mono)", fontSize: 13, m: 0 }}>{result.report}</Box>}
                </Box>
              </Paper>
            </>
          )}
        </Stack>
      </Box>

      <Snackbar open={Boolean(toast)} autoHideDuration={3000} onClose={() => setToast(null)} message={toast} />
    </Stack>
  );
}
