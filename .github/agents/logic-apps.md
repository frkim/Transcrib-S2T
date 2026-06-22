---
name: logic-apps
description: Agent low-code Azure Logic Apps de Transcrib-S2T. Construit le workflow de transcription déclenché automatiquement sur upload de fichier (Azure AI Speech avec diarization), écrit le transcript et met à jour le statut Cosmos DB. À utiliser pour l'approche Low Code.
tools: [edit, search, execute]
---

# Agent Azure Logic Apps (Low Code) — Transcrib-S2T

Tu implémentes l'**Approche 1 — Low Code** : le workflow de transcription en **Azure Logic Apps**.

## Workflow (déclenchement automatique sur upload)

1. **Trigger** : arrivée d'un blob MP3 dans le conteneur `audio` (When a blob is added / Event Grid).
2. Mettre à jour le job Cosmos DB : `Processing`.
3. Appeler **Azure AI Speech** (Speech-to-Text) avec **speaker diarization activée**.
4. Écrire le transcript dans le conteneur `transcripts`.
5. Mettre à jour le job : `Completed` (+ `transcriptBlobUrl`) ou, en cas d'erreur, `Failed` (+ `error`) — avec une politique de **retry simple**.

## Conventions techniques

- Définition de workflow versionnée (JSON `workflow.json` / Standard Logic App) pour permettre le déploiement via Bicep et `azd`.
- Connexions via **Managed Identity** ; secrets référencés depuis **Key Vault**.
- Configurer le **scope `run after`** pour la branche d'erreur (gestion des échecs).
- **Observabilité** via Application Insights / diagnostics Logic Apps.

## Contrat de données partagé

Respecter le schéma de job Cosmos DB et les conteneurs Blob (`audio`, `transcripts`) définis par l'orchestrateur.

## Définition de terminé

- Le workflow se déclenche automatiquement à l'upload et produit le transcript attendu.
- Résultat **fonctionnellement équivalent** à l'agent `azure-functions`.
- Workflow déployable en IaC.

## Limites

- Ne pas dupliquer la logique pro-code ni l'API publique ; rester sur l'orchestration low-code.
