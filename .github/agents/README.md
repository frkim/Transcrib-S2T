# Agents GitHub Copilot — Transcrib-S2T

Ce dossier contient les **agents personnalisés** ([custom agents](https://docs.github.com/en/copilot/reference/custom-agents-configuration)) utilisés pour construire la solution Azure de transcription **Transcrib-S2T** (MP3 → texte, Speech-to-Text).

Chaque fichier `*.md` définit un agent spécialisé via un frontmatter YAML (`name`, `description`, `tools`, …) suivi de ses instructions. Le dossier est délibérément organisé autour d'un **agent orchestrateur** qui coordonne des **agents spécialisés**.

## Vue d'ensemble

| Agent | Fichier | Rôle |
| --- | --- | --- |
| Orchestrateur | [`orchestrator.md`](./orchestrator.md) | Planifie, séquence et délègue le travail aux agents spécialisés ; garantit la cohérence des contrats partagés. |
| Infrastructure | [`infrastructure.md`](./infrastructure.md) | Bicep + `azd` (Blob, Cosmos DB, AI Speech, Key Vault, App Insights, Container Apps, ACR, Entra ID). |
| Backend API | [`backend-api.md`](./backend-api.md) | API C# (.NET) sur Azure Container Apps. |
| Azure Functions | [`azure-functions.md`](./azure-functions.md) | Approche **Pro Code** : pipeline de transcription. |
| Logic Apps | [`logic-apps.md`](./logic-apps.md) | Approche **Low Code** : workflow de transcription. |
| Frontend | [`frontend.md`](./frontend.md) | Application web Next.js (upload, suivi, téléchargement). |
| Cycle de vie / Purge | [`lifecycle-purge.md`](./lifecycle-purge.md) | Logic App quotidienne de purge (> 1 jour). |
| QA & Tests | [`qa-testing.md`](./qa-testing.md) | Tests unitaires essentiels et observabilité. |

## Flux d'orchestration recommandé

```
orchestrator
   ├─ infrastructure        (provisionne les ressources Azure)
   ├─ backend-api           (API C# sur Container Apps)
   ├─ azure-functions  ┐    (transcription — Pro Code)
   ├─ logic-apps       ┘    (transcription — Low Code, équivalent)
   ├─ frontend              (Next.js)
   ├─ lifecycle-purge       (purge quotidienne)
   └─ qa-testing            (tests + observabilité)
```

## Contrats partagés

Pour garantir l'interopérabilité, tous les agents respectent les conventions définies dans [`orchestrator.md`](./orchestrator.md) :

- **Conteneurs Blob** : `audio` (sources MP3) et `transcripts` (résultats).
- **Cosmos DB** : conteneur `jobs` avec les champs `id`, `fileName`, `audioBlobUrl`, `transcriptBlobUrl`, `status`, `error`, `createdAt`, `updatedAt`.
- **Statuts** : `Processing | Completed | Failed | Purged`.
- **Secrets** : toujours via **Key Vault**.
- **Observabilité** : logs et traces via **Application Insights**.

## Utilisation

Ces agents peuvent être sélectionnés dans GitHub Copilot (cloud agent / IDE compatibles). L'orchestrateur peut être invoqué pour les demandes de bout en bout ; les agents spécialisés pour des tâches ciblées.
