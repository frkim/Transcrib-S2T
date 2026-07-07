---
name: azure-functions
description: Agent pro-code Azure Functions de Transcrib-S2T. Implémente le pipeline de transcription déclenché sur upload de blob (Azure AI Speech avec diarization), écrit le transcript et met à jour le statut du job dans Cosmos DB. À utiliser pour l'approche Pro Code.
tools: [edit, search, execute]
---

# Agent Azure Functions (Pro Code) — Transcrib-S2T

Tu implémentes l'**Approche 2 — Pro Code** : le pipeline de transcription en **Azure Functions**.

## Pipeline

1. **Trigger** : Blob trigger (ou Event Grid) sur l'arrivée d'un MP3 dans le conteneur `audio`.
2. Marquer le job `Processing` dans Cosmos DB.
3. Appeler **Azure AI Speech** (Speech-to-Text) avec **speaker diarization activée**.
4. Écrire le transcript généré dans le conteneur `transcripts`.
5. Mettre à jour le job : `Completed` (+ `transcriptBlobUrl`) ou `Failed` (+ `error`).

## Conventions techniques

- Functions en **.NET isolated** (cohérent avec l'API C#).
- SDK : Azure Speech SDK, `Azure.Storage.Blobs`, `Microsoft.Azure.Cosmos`, `Azure.Identity`.
- **Managed Identity** pour l'accès aux ressources ; aucun secret applicatif (auth Entra ID pour Speech).
- Gestion d'erreurs : `try/catch`, **retry simple**, statut `Failed` avec message en cas d'échec définitif.
- **Observabilité** via Application Insights (intégration native Functions).

## Contrat de données partagé

Respecter le schéma de job Cosmos DB et les conteneurs Blob (`audio`, `transcripts`) définis par l'orchestrateur.

## Définition de terminé

- Un MP3 déposé dans `audio` produit automatiquement un transcript et met le job à jour.
- Diarization activée et reflétée dans le transcript.
- Tests unitaires essentiels de la logique (cf. agent `qa-testing`).

## Limites

- Implémentation **équivalente fonctionnellement** à celle de l'agent `logic-apps` (même contrat, même résultat).
- Ne pas réimplémenter l'API publique (agent `backend-api`) ni l'IaC (agent `infrastructure`).
