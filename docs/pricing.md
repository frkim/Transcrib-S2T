# Estimation des coûts — Transcrib-S2T

Cette page estime le coût mensuel de la solution Transcrib-S2T sur Azure dans
deux configurations :

1. **Non-production** (dev / démo / recette) — l'infrastructure telle que
   provisionnée par `azd up` dans ce dépôt (`infra/`).
2. **Production sécurisée** — la même architecture renforcée (réseau privé,
   WAF, haute disponibilité, Defender for Cloud, rétention des logs…).

Elle détaille aussi le **coût de la transcription vocale → texte** (Azure AI
Speech), qui est le seul poste réellement proportionnel au volume d'audio, et
une **hypothèse haute « centre d'appel »** (DMT 7 minutes, 5 millions d'audios
par an).

> ⚠️ **Estimations indicatives**, en **euros hors taxes par mois (€ HT/mois)**,
> basées sur les prix publics *pay-as-you-go* (hors remises, réservations,
> EA/MCA ou taxes) pour la région **France Central**, région par défaut du
> projet (alternatives : **Sweden Central**, puis **North Europe** — voir
> [Région de déploiement](#région-de-déploiement)). Les montants sont arrondis
> (base de conversion de la grille publique : 1 USD ≈ 0,92 EUR). Les prix Azure
> évoluent : valider systématiquement avec
> l'[Azure Pricing Calculator](https://azure.microsoft.com/fr-fr/pricing/calculator/)
> (devise **Euro (€)**, région **France Central**) et la page
> [Tarifs Azure AI Speech](https://azure.microsoft.com/fr-fr/pricing/details/cognitive-services/speech-services/).

## Sommaire

- [Hypothèses communes](#hypothèses-communes)
- [Région de déploiement](#région-de-déploiement)
- [Coût de la transcription (Azure AI Speech)](#coût-de-la-transcription-azure-ai-speech)
- [Environnement non-production](#environnement-non-production)
- [Environnement de production sécurisé](#environnement-de-production-sécurisé)
- [Hypothèse centre d'appel (DMT 7 min, 5 M audios/an)](#hypothèse-centre-dappel-dmt-7-min-5-m-audiosan)
- [Variante centre d'appel : analyse échantillonnée](#variante-centre-dappel--analyse-échantillonnée)
- [Synthèse](#synthèse)
- [Leviers d'optimisation](#leviers-doptimisation)

## Hypothèses communes

- Région **France Central** par défaut (alternatives : Sweden Central, puis
  North Europe), un seul environnement par estimation.
- Montants en **euros hors taxes** (€ HT), arrondis.
- Fichiers **MP3** (~128 kbit/s ≈ 1 Mo/min, soit ~60 Mo par heure d'audio).
- Audio et transcripts **purgés après 1 jour** (Logic App de purge) : le volume
  stocké reste donc très faible quel que soit le volume transcrit.
- Transcription via l'**API Fast Transcription** avec *speaker diarization*
  (voir [architecture.md](architecture.md#choix-du-service-azure-speech)).
- L'**analyse qualitative** (`/analysis`) s'exécute **dans le navigateur** :
  aucun service d'IA supplémentaire (Azure OpenAI, etc.) n'est facturé.
- Quotas par défaut de l'API (`TranscriptionLimits`) : 3 transcriptions/jour,
  10/semaine, 5 minutes max par fichier.

## Région de déploiement

La région est choisie à la création de l'environnement `azd` (variable
`AZURE_LOCATION`) ; toutes les ressources sont déployées dans cette région.
Ordre de préférence :

| Priorité | Région (`AZURE_LOCATION`) | Localisation des données | Quand la retenir | Prix indicatifs vs France Central |
| --- | --- | --- | --- | --- |
| **1 — par défaut** | **France Central** (`francecentral`) | Paris, France | Hébergement des données en France (exigence de conformité du projet). | Référence des estimations de cette page |
| 2 — alternative | Sweden Central (`swedencentral`) | Suède (UE) | Service, SKU, capacité ou quota indisponible en France Central (ex. quota Azure AI Speech, Functions Flex Consumption). | Speech généralement identique ; calcul, stockage et réseau identiques ou légèrement inférieurs (quelques %) |
| 3 — alternative | North Europe (`northeurope`) | Irlande (UE) | Si Sweden Central ne convient pas non plus. | Speech généralement identique ; calcul, stockage et réseau identiques ou légèrement inférieurs (quelques %) |

```bash
azd env new <nom-environnement> --location francecentral   # par défaut
# azd env new <nom-environnement> --location swedencentral # alternative 1
# azd env new <nom-environnement> --location northeurope   # alternative 2
```

Pour le workflow GitHub Actions (`azure-dev.yml`), renseigner la variable de
dépôt `AZURE_LOCATION` avec la même valeur.

> Les estimations en France Central constituent un **majorant raisonnable**
> pour Sweden Central et North Europe. Ces deux régions restent dans l'Union
> européenne (RGPD) mais **hors de France** : leur utilisation doit être validée
> avec le DPO / RSSI au regard de l'exigence d'hébergement en France.

## Coût de la transcription (Azure AI Speech)

### Grille tarifaire Speech-to-Text (S0, pay-as-you-go)

| Mode Speech-to-Text | Prix public indicatif | Commentaire |
| --- | ---: | --- |
| **Fast Transcription** (utilisé par le projet) | **~0,92 € / heure d'audio** (≈ 0,0153 €/min) | Synchrone, facturé à la seconde d'audio. |
| Temps réel (standard) | ~0,92 € / heure | Streaming (SDK), non utilisé. |
| Batch Transcription | ~0,17 € / heure | Asynchrone (soumission + *polling*) — ~80 % moins cher. |
| Modèle personnalisé (Custom Speech) | ~1,10 € / heure + hébergement du endpoint (~0,05 €/h) | Uniquement si un vocabulaire métier spécifique est nécessaire. |
| *Speaker diarization* | **Inclus** | Pas de surcoût. |
| Niveau gratuit (F0) | 5 h d'audio / mois | Possible en dev, limité en concurrence ; la ressource du projet est en **S0**. |
| Engagement (*commitment tier*) | ex. 2 000 h ≈ 1 470 € (~0,74 €/h) ; 50 000 h ≈ 23 000 € (~0,46 €/h) | Rentable au-delà de ~1 500 h/mois régulières. |

### Coût marginal par heure d'audio

| Poste | Coût par heure d'audio transcrite |
| --- | ---: |
| Azure AI Speech (Fast Transcription) | ~0,92 € |
| Blob Storage (≈ 60 Mo MP3 + transcript, conservés 1 jour, opérations) | < 0,001 € |
| Azure Functions / Logic Apps (1 exécution par fichier) | < 0,001 € |
| Cosmos DB serverless (quelques écritures/lectures de job) | < 0,001 € |
| Event Grid (1 événement par fichier) | ~0 € |
| **Total marginal** | **≈ 0,92 € / heure d'audio (≈ 0,015 € / minute)** |

> Le coût marginal est **quasi entièrement** celui d'Azure AI Speech : les autres
> services sont négligeables à l'unité grâce à la purge quotidienne.

### Scénarios de volume

| Scénario | Volume audio / mois | Fast Transcription (pay-as-you-go) | Équivalent Batch (pour comparaison) |
| --- | ---: | ---: | ---: |
| Quotas par défaut (10 fichiers × 5 min / semaine) | ~3,6 h | **~3,3 €** | ~0,6 € |
| Petite équipe (20 appels × 5 min / jour ouvré) | ~37 h | **~34 €** | ~6 € |
| Service client (200 appels × 10 min / jour ouvré) | ~730 h | **~670 €** | ~120 € |
| Centre de contact (2 000 h / mois) | 2 000 h | **~1 840 €** (≈ 1 470 € avec engagement) | ~330 € |
| **Centre d'appel — hypothèse max** (5 M audios/an × DMT 7 min, voir [détail](#hypothèse-centre-dappel-dmt-7-min-5-m-audiosan)) | ~48 600 h | **~44 700 €** (≈ 23 000 € avec engagement 50 000 h) | ~8 050 € |

> Au-delà de quelques centaines d'heures par mois, il est pertinent d'évaluer
> un *commitment tier* ou un passage partiel en **Batch Transcription** (au prix
> d'une latence plus élevée et d'une orchestration asynchrone). Les quotas de
> l'API (`TranscriptionLimits`) constituent par ailleurs un garde-fou budgétaire.

## Environnement non-production

Configuration actuellement déployée par `infra/` : services *serverless*,
scale-to-zero, endpoints publics protégés par Managed Identity / Entra ID,
Microsoft Defender for Cloud désactivé, rétention des logs 30 jours.

| Service | SKU / configuration (`infra/`) | Base de facturation | Coût approx. (€/mois) |
| --- | --- | --- | ---: |
| Azure Container Apps (frontend + API) | Consumption, `minReplicas: 0`, 0,5 vCPU / 1 Gi | vCPU-s / GiB-s (180 000 vCPU-s et 360 000 GiB-s gratuits/mois) | ~0–3 |
| Azure Container Registry | Basic | Forfait (~0,15 €/jour) | ~5 |
| Azure Functions | Flex Consumption (FC1) | GB-s + exécutions (franchise mensuelle) | ~0–2 |
| Azure Logic Apps (transcription + purge) | Consumption | Par action exécutée | ~0–1 |
| Azure AI Speech | S0, Fast Transcription | Par heure d'audio | ~1–5 (quotas par défaut) |
| Azure Cosmos DB | Serverless | Par RU consommée + Go stockés | ~0–1 |
| Azure Blob Storage (2 comptes) | Standard LRS, Hot | Go stockés + opérations | ~0,5–1 |
| Azure Event Grid | System topic | Par opération (100 000 gratuites/mois) | ~0 |
| Log Analytics + Application Insights | PerGB2018, rétention 30 jours | Par Go ingéré (5 Go gratuits/mois) | ~1–3 |
| User-assigned Managed Identity | — | — | Gratuit |
| Entra ID (inscription d'application) | Niveau gratuit | — | Gratuit |
| **Total non-production** | | | **~7–18 €/mois** |

> Le poste dominant à faible charge est le **Container Registry Basic**
> (forfait fixe). Le coût Speech reste faible tant que les quotas par défaut
> sont conservés.

## Environnement de production sécurisé

### Éléments de sécurité ajoutés

Par rapport à la non-production, la configuration de production recommandée
ajoute :

- **Isolation réseau** : VNet dédié, **Private Endpoints** pour Blob (données
  et Functions), Cosmos DB, Azure AI Speech, Container Registry et Key Vault ;
  accès public désactivé sur ces ressources ; zones **Private DNS**.
- **Container Apps** dans un environnement *workload profiles* intégré au VNet,
  `minReplicas ≥ 1` (pas de démarrage à froid) et **zone redundancy**.
- **Exposition web** via **Azure Front Door Premium + WAF** (règles managées
  OWASP, *bot protection*) avec Private Link vers les Container Apps ; CORS
  restreint au domaine du frontend.
- **Azure Functions Flex Consumption** avec **intégration VNet** et une
  instance *always ready*.
- **Container Registry Premium** (requis pour les Private Endpoints).
- **NAT Gateway** pour une IP de sortie fixe et maîtrisée.
- **Key Vault** pour les éventuels secrets/certificats (domaines personnalisés).
- **Microsoft Defender for Cloud** (Storage, Containers, Key Vault, Cosmos DB,
  Resource Manager).
- **Stockage ZRS** avec *soft delete* ; Cosmos DB avec sauvegarde continue
  (7 jours, incluse).
- **Logs** : rétention étendue (ex. 90 jours) et volume d'ingestion plus élevé
  (traces, diagnostics, journaux d'accès WAF).
- **Purge** : les Logic Apps *Consumption* ne peuvent pas joindre des ressources
  derrière Private Endpoint. En production, privilégier le pipeline **Pro Code**
  (Functions) et réaliser la purge via une **politique de cycle de vie Blob**
  (gratuite) ou une Function *timer* ; à défaut, migrer vers **Logic Apps
  Standard (WS1)** intégré au VNet.

### Estimation détaillée

| Service | Configuration production | Coût approx. (€/mois) |
| --- | --- | ---: |
| Azure Front Door Premium + WAF | Forfait de base + requêtes/transfert (faible trafic) | ~310–370 |
| Azure Container Apps (frontend + API) | Workload profiles (profil Consumption), VNet, `minReplicas: 1–2`, zone redundancy | ~65–140 |
| Azure Functions | Flex Consumption + intégration VNet + 1 instance *always ready* | ~18–37 |
| Purge | Politique de cycle de vie Blob / Function *timer* (option : Logic Apps Standard WS1 ~165 €) | ~0 (ou ~165) |
| Azure AI Speech | S0, Fast Transcription, accès public désactivé | **variable** — voir [scénarios](#scénarios-de-volume) |
| Azure Cosmos DB | Serverless (ou autoscale zone-redondant), sauvegarde continue 7 jours | ~5–28 |
| Azure Blob Storage (2 comptes) | Standard ZRS, *soft delete* | ~3–9 |
| Azure Container Registry | Premium | ~46 |
| Private Endpoints | ~9 endpoints (~6,70 €/mois chacun) + données traitées | ~60–69 |
| Private DNS zones | ~7 zones | ~4 |
| NAT Gateway | 1 passerelle + IP publique + données sortantes | ~32–41 |
| Azure Key Vault | Standard | ~1 |
| Log Analytics + Application Insights | 10–30 Go/mois, rétention 90 jours | ~23–83 |
| Microsoft Defender for Cloud | Storage (×2), Containers, Key Vault, Cosmos DB, Resource Manager | ~37–92 |
| Azure Event Grid | System topic | ~0–1 |
| User-assigned Managed Identity | — | Gratuit |
| Entra ID | Niveau gratuit (option P1 ~5,60 €/utilisateur/mois pour l'accès conditionnel) | 0 (+ licences) |
| **Total production sécurisée (hors Speech)** | | **~600–920 €/mois** |

> **Variante sans exposition Internet** (accès uniquement via VPN /
> ExpressRoute, ingress Container Apps interne) : sans Front Door Premium, le
> socle descend à **~295–550 €/mois** (hors Speech).
>
> **Options non incluses** : Azure DDoS Network Protection (~2 710 €/mois),
> Azure Firewall (~830 €/mois et plus), Microsoft Sentinel, Defender for Storage
> *malware scanning* (~0,14 €/Go analysé), multi-région / reprise d'activité.

## Hypothèse centre d'appel (DMT 7 min, 5 M audios/an)

Hypothèse **maximale** pour un usage de type **centre d'appel** : chaque appel
enregistré est transcrit, avec une **durée moyenne de traitement (DMT) de
7 minutes** et **5 millions d'audios par an**. Ce volume suppose la
configuration de **production sécurisée** décrite ci-dessus.

### Volumétrie

| Indicateur | Calcul | Valeur |
| --- | --- | ---: |
| Audios / an | hypothèse | **5 000 000** |
| Audios / mois | 5 000 000 ÷ 12 | ~416 700 |
| Audios / jour ouvré | 5 000 000 ÷ ~250 jours | ~20 000 (≈ 2 000/h sur 10 h, ~35/min) |
| Minutes d'audio / an | 5 000 000 × 7 min | 35 000 000 min |
| **Heures d'audio / an** | 35 000 000 ÷ 60 | **~583 300 h** |
| **Heures d'audio / mois** | 583 300 ÷ 12 | **~48 600 h** |
| Volume MP3 ingéré / mois | ~416 700 × ~7 Mo | ~2,9 To (stock moyen ~100–200 Go grâce à la purge quotidienne) |

### Coût Speech du centre d'appel

| Mode | Prix unitaire | Coût par audio (7 min) | Coût / mois | Coût / an |
| --- | ---: | ---: | ---: | ---: |
| Fast Transcription, *pay-as-you-go* (actuel) | ~0,92 €/h | ~0,107 € | **~44 700 €** | **~536 000 €** |
| Fast Transcription + *commitment tier* 50 000 h/mois¹ | ~0,46 €/h (forfait ~23 000 €/mois) | ~0,054 € | **~23 000 €** | **~276 000 €** |
| Batch Transcription² | ~0,17 €/h | ~0,019 € | **~8 050 €** | **~96 600 €** |

¹ Le palier 50 000 h/mois couvre les ~48 600 h mensuelles ; les heures au-delà
du forfait sont facturées au tarif de dépassement. Vérifier l'éligibilité de
Fast Transcription au *commitment tier* et les conditions (engagement annuel,
région) auprès de Microsoft.
² Nécessite une évolution de l'architecture (soumission asynchrone + *polling*,
latence de quelques minutes à quelques heures) — pertinent pour un traitement
différé des appels.

### Socle de production à cette échelle (hors Speech)

À ce volume, les postes « négligeables à l'unité » ne le sont plus en cumulé.
Ajustement du [socle de production sécurisée](#estimation-détaillée) :

| Service | Hypothèse de charge | Coût approx. (€/mois) |
| --- | --- | ---: |
| Azure Front Door Premium + WAF | Forfait + ~420 k uploads/mois (~2,9 To) + requêtes UI | ~370–600 |
| Azure Container Apps (frontend + API) | Plusieurs réplicas en heures ouvrées (uploads ~7 Mo) | ~140–275 |
| Azure Functions (Pro Code) | ~420 k exécutions/mois, chacune attend la réponse Speech (~30–90 s) | ~90–370 |
| Azure Cosmos DB | ~420 k jobs/mois (créations + mises à jour de statut + requêtes UI) | ~9–46 |
| Azure Blob Storage (2 comptes) | ~2,9 To écrits/mois, ~100–200 Go stockés, millions d'opérations | ~28–55 |
| Private Endpoints | ~9 endpoints + ~6–9 To de données traitées (~0,01 €/Go) | ~120–150 |
| Log Analytics + Application Insights | ~50–150 Go/mois (traces par job), rétention 90 jours | ~140–370 |
| Microsoft Defender for Cloud | Inchangé (hors *malware scanning*) | ~37–92 |
| Autres (ACR Premium, NAT Gateway, Private DNS, Key Vault, Event Grid, purge) | Inchangé ; Event Grid ~1 M opérations/mois | ~83–92 |
| **Total socle production (hors Speech)** | | **~1 000–2 100 €/mois** |

> Avec l'approche *Low Code* (Logic Apps Consumption, ~15–20 actions par
> fichier), compter **~370–740 €/mois** supplémentaires pour la transcription ;
> l'approche *Pro Code* (Functions) est recommandée à ce volume.
>
> Option : Defender for Storage *malware scanning* sur les uploads
> (~2,9 To × ~0,14 €/Go) ≈ **+400 €/mois**.

### Total hypothèse centre d'appel

| Option Speech | Total / mois (€) | Total / an (€) | Coût complet par audio |
| --- | ---: | ---: | ---: |
| Fast Transcription *pay-as-you-go* | **~45 700–46 800** | **~548 000–562 000** | ~0,11 € |
| Fast Transcription + engagement 50 000 h | **~24 000–25 100** | **~288 000–301 000** | ~0,06 € |
| Batch Transcription | **~9 050–10 150** | **~109 000–122 000** | ~0,023 € |

> Avec une DMT de 7 minutes et 5 M d'audios/an, **Speech représente ~85–97 %**
> de la facture selon l'option : le choix du mode de facturation (engagement)
> ou du mode de transcription (Batch) est le principal levier.

### Prérequis de configuration

- **Quotas de l'API** (`TranscriptionLimits`, appliqués globalement) : les
  valeurs par défaut bloqueraient ce scénario. Relever `MaxDurationMinutes`
  au-delà de la DMT (ex. **30 minutes**, la DMT étant une moyenne),
  `MaxPerDay` (ex. **≥ 25 000**) et `MaxPerWeek` (ex. **≥ 120 000**).
- **Quotas Azure AI Speech** : vérifier les limites de requêtes / de
  concurrence de la ressource S0 pour ~35 transcriptions/min en pointe (et plus
  lors des pics d'appels) et demander une augmentation si nécessaire.
- **Ingestion** : à ce volume, l'alimentation est généralement automatisée
  depuis la plateforme de téléphonie (appels à l'API `POST /jobs`) plutôt que
  via l'interface web.

## Variante centre d'appel : analyse échantillonnée

L'[hypothèse centre d'appel](#hypothèse-centre-dappel-dmt-7-min-5-m-audiosan)
transcrit **100 % des appels**. Cette variante ne transcrit qu'un **échantillon**
des appels (ex. 15 %) pour réduire la facture. Le coût Speech étant strictement
proportionnel au volume d'audio, l'échantillonnage est le levier le plus direct,
devant le *commitment tier*.

> ⚠️ Cette variante **déroge à l'objectif O1** du
> [dossier projet](dossier-projet.md#21-contexte-et-objectifs) (« couvrir
> l'ensemble des conversations, et non un échantillon ») : elle est à présenter
> comme une **option budgétaire** ou une **phase de montée en charge**
> (ex. démarrer à 15 %, puis étendre), pas comme la cible.

Hypothèses : celles du centre d'appel (5 M audios/an, DMT 7 min) ;
échantillonnage réalisé **à la source** (plateforme de téléphonie ou script
d'ingestion, avant `POST /jobs`), de sorte que les audios non retenus ne sont
ni téléversés, ni stockés, ni traités. Le socle de production sécurisée
(~600–920 €/mois) reste fixe ; sa part variable (Front Door, Functions, Blob,
Private Endpoints, logs…) est extrapolée linéairement entre le faible volume et
le [socle à pleine échelle](#socle-de-production-à-cette-échelle-hors-speech).

### Volumétrie et coût Speech par taux d'échantillonnage

| Taux | Audios / mois | Heures d'audio / mois | Speech Fast *pay-as-you-go* (€/mois) | Speech Batch (€/mois) |
| ---: | ---: | ---: | ---: | ---: |
| 5 % | ~20 800 | ~2 430 h | ~2 240 | ~400 |
| 10 % | ~41 700 | ~4 860 h | ~4 470 | ~800 |
| **15 %** | **~62 500** | **~7 290 h** | **~6 710** | **~1 210** |
| 25 % | ~104 200 | ~12 150 h | ~11 180 | ~2 010 |
| 50 % | ~208 300 | ~24 300 h | ~22 360 | ~4 020 |
| 100 % (référence) | ~416 700 | ~48 600 h | ~44 700 | ~8 050 |

> **Engagement** : le palier 50 000 h/mois n'est plus adapté en dessous de
> ~100 % de couverture. Dès ~2 000 h/mois (≈ 4 % d'échantillonnage), un palier
> inférieur peut être plus rentable que le *pay-as-you-go* : prix des paliers
> intermédiaires à valider avec
> l'[Azure Pricing Calculator](https://azure.microsoft.com/fr-fr/pricing/calculator/).

### Coût total par taux d'échantillonnage (production sécurisée)

| Taux | Socle hors Speech (€/mois) | Total Fast (€/mois) | Total Fast (€/an) | Total Batch (€/mois) | Total Batch (€/an) | Coût complet par audio transcrit (Fast / Batch) |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 5 % | ~620–980 | ~2 860–3 220 | ~34–39 k | ~1 020–1 380 | ~12–17 k | ~0,15 € / ~0,06 € |
| 10 % | ~640–1 040 | ~5 110–5 510 | ~61–66 k | ~1 450–1 840 | ~17–22 k | ~0,13 € / ~0,04 € |
| **15 %** | **~660–1 100** | **~7 370–7 800** | **~88–94 k** | **~1 870–2 300** | **~22–28 k** | **~0,12 € / ~0,033 €** |
| 25 % | ~700–1 220 | ~11 880–12 390 | ~143–149 k | ~2 710–3 230 | ~33–39 k | ~0,12 € / ~0,029 € |
| 50 % | ~800–1 510 | ~23 160–23 870 | ~278–286 k | ~4 830–5 540 | ~58–66 k | ~0,11 € / ~0,025 € |
| 100 % (référence) | ~1 000–2 100 | ~45 700–46 800 | ~548–562 k | ~9 050–10 150 | ~109–122 k | ~0,11 € / ~0,023 € |

> La facture baisse presque proportionnellement au taux, mais le **coût par
> audio transcrit augmente** aux faibles taux, car le socle de sécurité est
> réparti sur moins d'appels.

### Représentativité statistique

Le bon taux dépend de la **maille d'analyse la plus fine** visée, pas du volume
global. Marge d'erreur à 95 % sur une proportion (ex. taux d'appels
non conformes, pire cas p = 50 %, avec correction de population finie), avec
une hypothèse illustrative de **~500 conseillers** (~830 appels par conseiller
et par mois) :

| Taux | Audios / mois | Marge globale (mois) | Audios / conseiller / mois | Marge par conseiller (mois) |
| ---: | ---: | ---: | ---: | ---: |
| 5 % | ~20 800 | ±0,7 pt | ~42 | ±15 pts |
| 10 % | ~41 700 | ±0,5 pt | ~83 | ±10 pts |
| 15 % | ~62 500 | ±0,4 pt | ~125 | ±8 pts |
| 25 % | ~104 200 | ±0,3 pt | ~208 | ±6 pts |
| 50 % | ~208 300 | ±0,15 pt | ~417 | ±3 pts |

### Recommandations

- **Pilotage global** (tendances, motifs d'appel, irritants) : **5–10 %**
  suffisent largement.
- **Coaching individuel** (indicateurs mensuels par conseiller) : **15–25 %**,
  ou mieux un **échantillonnage stratifié** à quota fixe (ex. 100–150 appels
  par conseiller et par mois, soit ~12–18 % avec ~500 conseillers), qui garantit
  la même précision pour chaque conseiller ou chaque file.
- **Modèle hybride** : transcrire **100 % des appels ciblés** (réclamations,
  escalades, appels longs, ventes soumises à conformité) et un échantillon
  aléatoire du reste.
- **Rester à 100 %** si le besoin est exhaustif : obligations réglementaires,
  litiges, recherche dans l'ensemble des appels, détection systématique des
  signaux faibles.
- **Mise en œuvre** : sélection aléatoire reproductible à la source (ex. hachage
  de l'identifiant d'appel), taux tracé pour redresser les indicateurs. Aucune
  évolution de l'API n'est requise ; ajuster les quotas `TranscriptionLimits`
  au volume échantillonné (ex. à 15 % : ~3 000 audios/jour ouvré, soit
  `MaxPerDay` ≥ 4 000 et `MaxPerWeek` ≥ 20 000).
- **Retour sur investissement** : le gain de productivité du
  [dossier projet](dossier-projet.md#8-valeur-et-retour-sur-investissement)
  (évaluation de 1 % des appels) reste acquis tant que le taux est ≥ 1 % et que
  les appels évalués font partie de l'échantillon. À 15 % en Batch, le coût de
  fonctionnement passe à ~55–60 k€/an (Azure + MCO) et le retour sur
  investissement à **~5 mois** (au lieu de ~7–8), au prix de la perte de la
  couverture exhaustive.

## Synthèse

| Volume audio / mois | Non-production | Production sécurisée |
| ---: | ---: | ---: |
| ~4 h (quotas par défaut) | ~9–18 € | ~605–925 € |
| ~40 h | ~40–50 € | ~635–955 € |
| ~730 h | ~675–685 € | ~1 270–1 590 € |
| 2 000 h | ~1 845–1 855 € | ~2 440–2 760 € (≈ 2 070–2 390 € avec engagement Speech) |
| ~7 300 h (centre d'appel échantillonné à 15 %, voir [variante](#variante-centre-dappel--analyse-échantillonnée)) | — (volume de production) | ~7 370–7 800 € (≈ 1 870–2 300 € en Batch) |
| ~48 600 h (centre d'appel : 5 M audios/an × DMT 7 min) | — (volume de production) | ~45 700–46 800 € (≈ 24 000–25 100 € avec engagement 50 000 h ; ≈ 9 050–10 150 € en Batch) |

- En **non-production**, le coût fixe est minime (~9 €/mois) : la facture suit
  presque uniquement le volume transcrit (~0,92 € par heure d'audio).
- En **production sécurisée**, le coût est dominé par le **socle de sécurité**
  (Front Door/WAF, Private Endpoints, ACR Premium, Defender, logs) tant que le
  volume audio reste inférieur à ~700 h/mois ; au-delà, **Speech** devient le
  premier poste.
- Dans l'**hypothèse centre d'appel** (5 M audios/an, DMT 7 min), le budget
  annuel varie de **~555 k€** (*pay-as-you-go*) à **~295 k€** (engagement
  50 000 h) voire **~115 k€** (Batch Transcription). En
  [échantillonnant 15 % des appels](#variante-centre-dappel--analyse-échantillonnée),
  il descend à **~90 k€** (*pay-as-you-go*) ou **~25 k€** (Batch).

## Leviers d'optimisation

- **Speech** : conserver les quotas `TranscriptionLimits` comme garde-fou,
  souscrire un *commitment tier* pour un volume régulier élevé, ou basculer les
  traitements non urgents en **Batch Transcription** (~0,17 €/h). Pour un
  centre d'appel (~48 600 h/mois), le palier d'engagement 50 000 h divise
  quasiment par deux la facture Speech, et le Batch la divise par ~5,5.
- **Échantillonnage** : si la couverture exhaustive n'est pas requise,
  transcrire un échantillon (ex. 15 %, ou un quota par conseiller) divise la
  facture Speech d'autant ; cumulable avec le Batch (voir
  [variante échantillonnée](#variante-centre-dappel--analyse-échantillonnée)).
- **Non-production** : garder le scale-to-zero, la purge quotidienne et Defender
  désactivé ; supprimer l'environnement (`azd down`) lorsqu'il n'est pas utilisé.
- **Production** : mutualiser Front Door / Defender / Log Analytics avec
  d'autres applications, ajuster la rétention et l'échantillonnage
  d'Application Insights, et étudier les réservations (Container Apps,
  Cosmos DB) pour les charges stables.
- Mettre en place des **budgets et alertes Azure Cost Management** par
  environnement (groupe de ressources), en euros.
- **Région** : conserver France Central par défaut ; ne basculer vers Sweden
  Central (puis North Europe) qu'en cas d'indisponibilité de service ou de
  quota, après validation de la conformité (voir
  [Région de déploiement](#région-de-déploiement)). L'économie attendue sur le
  socle reste de l'ordre de quelques %.

## Références

- Architecture détaillée : [architecture.md](architecture.md).
- Dossier projet et offre financière : [dossier-projet.md](dossier-projet.md).
- Vue d'ensemble et déploiement : [../README.md](../README.md).
- [Azure Pricing Calculator](https://azure.microsoft.com/fr-fr/pricing/calculator/) (devise Euro, région France Central).
- [Tarifs Azure AI Speech](https://azure.microsoft.com/fr-fr/pricing/details/cognitive-services/speech-services/).
- [Tarifs Azure Container Apps](https://azure.microsoft.com/fr-fr/pricing/details/container-apps/).
- [Tarifs Azure Front Door](https://azure.microsoft.com/fr-fr/pricing/details/frontdoor/).
- [Tarifs Private Link](https://azure.microsoft.com/fr-fr/pricing/details/private-link/).
- [Tarifs Microsoft Defender for Cloud](https://azure.microsoft.com/fr-fr/pricing/details/defender-for-cloud/).
- [Régions prises en charge par Azure AI Speech](https://learn.microsoft.com/fr-fr/azure/ai-services/speech-service/regions).
