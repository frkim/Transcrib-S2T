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

> ⚠️ **Estimations indicatives**, en **USD/mois**, basées sur les prix publics
> *pay-as-you-go* (hors remises, réservations, EA/MCA ou taxes) pour la région
> **France Central**. Les prix Azure évoluent : valider systématiquement avec
> l'[Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
> et la page [Tarifs Azure AI Speech](https://azure.microsoft.com/pricing/details/cognitive-services/speech-services/).
> Ordre de grandeur de conversion : 1 USD ≈ 0,90–0,95 EUR.

## Sommaire

- [Hypothèses communes](#hypothèses-communes)
- [Coût de la transcription (Azure AI Speech)](#coût-de-la-transcription-azure-ai-speech)
- [Environnement non-production](#environnement-non-production)
- [Environnement de production sécurisé](#environnement-de-production-sécurisé)
- [Hypothèse centre d'appel (DMT 7 min, 5 M audios/an)](#hypothèse-centre-dappel-dmt-7-min-5-m-audiosan)
- [Synthèse](#synthèse)
- [Leviers d'optimisation](#leviers-doptimisation)

## Hypothèses communes

- Région **France Central**, un seul environnement par estimation.
- Fichiers **MP3** (~128 kbit/s ≈ 1 Mo/min, soit ~60 Mo par heure d'audio).
- Audio et transcripts **purgés après 1 jour** (Logic App de purge) : le volume
  stocké reste donc très faible quel que soit le volume transcrit.
- Transcription via l'**API Fast Transcription** avec *speaker diarization*
  (voir [architecture.md](architecture.md#choix-du-service-azure-speech)).
- L'**analyse qualitative** (`/analysis`) s'exécute **dans le navigateur** :
  aucun service d'IA supplémentaire (Azure OpenAI, etc.) n'est facturé.
- Quotas par défaut de l'API (`TranscriptionLimits`) : 3 transcriptions/jour,
  10/semaine, 5 minutes max par fichier.

## Coût de la transcription (Azure AI Speech)

### Grille tarifaire Speech-to-Text (S0, pay-as-you-go)

| Mode Speech-to-Text | Prix public indicatif | Commentaire |
| --- | ---: | --- |
| **Fast Transcription** (utilisé par le projet) | **~1,00 $ / heure d'audio** (≈ 0,0167 $/min) | Synchrone, facturé à la seconde d'audio. |
| Temps réel (standard) | ~1,00 $ / heure | Streaming (SDK), non utilisé. |
| Batch Transcription | ~0,18 $ / heure | Asynchrone (soumission + *polling*) — ~80 % moins cher. |
| Modèle personnalisé (Custom Speech) | ~1,20 $ / heure + hébergement du endpoint (~0,05 $/h) | Uniquement si un vocabulaire métier spécifique est nécessaire. |
| *Speaker diarization* | **Inclus** | Pas de surcoût. |
| Niveau gratuit (F0) | 5 h d'audio / mois | Possible en dev, limité en concurrence ; la ressource du projet est en **S0**. |
| Engagement (*commitment tier*) | ex. 2 000 h ≈ 1 600 $ (~0,80 $/h) ; 50 000 h ≈ 25 000 $ (~0,50 $/h) | Rentable au-delà de ~1 500 h/mois régulières. |

### Coût marginal par heure d'audio

| Poste | Coût par heure d'audio transcrite |
| --- | ---: |
| Azure AI Speech (Fast Transcription) | ~1,00 $ |
| Blob Storage (≈ 60 Mo MP3 + transcript, conservés 1 jour, opérations) | < 0,001 $ |
| Azure Functions / Logic Apps (1 exécution par fichier) | < 0,001 $ |
| Cosmos DB serverless (quelques écritures/lectures de job) | < 0,001 $ |
| Event Grid (1 événement par fichier) | ~0 $ |
| **Total marginal** | **≈ 1,00 $ / heure d'audio (≈ 0,017 $ / minute)** |

> Le coût marginal est **quasi entièrement** celui d'Azure AI Speech : les autres
> services sont négligeables à l'unité grâce à la purge quotidienne.

### Scénarios de volume

| Scénario | Volume audio / mois | Fast Transcription (pay-as-you-go) | Équivalent Batch (pour comparaison) |
| --- | ---: | ---: | ---: |
| Quotas par défaut (10 fichiers × 5 min / semaine) | ~3,6 h | **~3,6 $** | ~0,7 $ |
| Petite équipe (20 appels × 5 min / jour ouvré) | ~37 h | **~37 $** | ~7 $ |
| Service client (200 appels × 10 min / jour ouvré) | ~730 h | **~730 $** | ~130 $ |
| Centre de contact (2 000 h / mois) | 2 000 h | **~2 000 $** (≈ 1 600 $ avec engagement) | ~360 $ |
| **Centre d'appel — hypothèse max** (5 M audios/an × DMT 7 min, voir [détail](#hypothèse-centre-dappel-dmt-7-min-5-m-audiosan)) | ~48 600 h | **~48 600 $** (≈ 25 000 $ avec engagement 50 000 h) | ~8 750 $ |

> Au-delà de quelques centaines d'heures par mois, il est pertinent d'évaluer
> un *commitment tier* ou un passage partiel en **Batch Transcription** (au prix
> d'une latence plus élevée et d'une orchestration asynchrone). Les quotas de
> l'API (`TranscriptionLimits`) constituent par ailleurs un garde-fou budgétaire.

## Environnement non-production

Configuration actuellement déployée par `infra/` : services *serverless*,
scale-to-zero, endpoints publics protégés par Managed Identity / Entra ID,
Microsoft Defender for Cloud désactivé, rétention des logs 30 jours.

| Service | SKU / configuration (`infra/`) | Base de facturation | Coût approx. (USD/mois) |
| --- | --- | --- | ---: |
| Azure Container Apps (frontend + API) | Consumption, `minReplicas: 0`, 0,5 vCPU / 1 Gi | vCPU-s / GiB-s (180 000 vCPU-s et 360 000 GiB-s gratuits/mois) | ~0–3 |
| Azure Container Registry | Basic | Forfait (~0,167 $/jour) | ~5 |
| Azure Functions | Flex Consumption (FC1) | GB-s + exécutions (franchise mensuelle) | ~0–2 |
| Azure Logic Apps (transcription + purge) | Consumption | Par action exécutée | ~0–1 |
| Azure AI Speech | S0, Fast Transcription | Par heure d'audio | ~1–5 (quotas par défaut) |
| Azure Cosmos DB | Serverless | Par RU consommée + Go stockés | ~0–1 |
| Azure Blob Storage (2 comptes) | Standard LRS, Hot | Go stockés + opérations | ~0,5–1 |
| Azure Event Grid | System topic | Par opération (100 000 gratuites/mois) | ~0 |
| Log Analytics + Application Insights | PerGB2018, rétention 30 jours | Par Go ingéré (5 Go gratuits/mois) | ~1–3 |
| User-assigned Managed Identity | — | — | Gratuit |
| Entra ID (inscription d'application) | Niveau gratuit | — | Gratuit |
| **Total non-production** | | | **~8–20 USD/mois** |

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

| Service | Configuration production | Coût approx. (USD/mois) |
| --- | --- | ---: |
| Azure Front Door Premium + WAF | Forfait de base + requêtes/transfert (faible trafic) | ~335–400 |
| Azure Container Apps (frontend + API) | Workload profiles (profil Consumption), VNet, `minReplicas: 1–2`, zone redundancy | ~70–150 |
| Azure Functions | Flex Consumption + intégration VNet + 1 instance *always ready* | ~20–40 |
| Purge | Politique de cycle de vie Blob / Function *timer* (option : Logic Apps Standard WS1 ~180 $) | ~0 (ou ~180) |
| Azure AI Speech | S0, Fast Transcription, accès public désactivé | **variable** — voir [scénarios](#scénarios-de-volume) |
| Azure Cosmos DB | Serverless (ou autoscale zone-redondant), sauvegarde continue 7 jours | ~5–30 |
| Azure Blob Storage (2 comptes) | Standard ZRS, *soft delete* | ~3–10 |
| Azure Container Registry | Premium | ~50 |
| Private Endpoints | ~9 endpoints (~7,30 $/mois chacun) + données traitées | ~65–75 |
| Private DNS zones | ~7 zones | ~4 |
| NAT Gateway | 1 passerelle + IP publique + données sortantes | ~35–45 |
| Azure Key Vault | Standard | ~1 |
| Log Analytics + Application Insights | 10–30 Go/mois, rétention 90 jours | ~25–90 |
| Microsoft Defender for Cloud | Storage (×2), Containers, Key Vault, Cosmos DB, Resource Manager | ~40–100 |
| Azure Event Grid | System topic | ~0–1 |
| User-assigned Managed Identity | — | Gratuit |
| Entra ID | Niveau gratuit (option P1 ~6 $/utilisateur/mois pour l'accès conditionnel) | 0 (+ licences) |
| **Total production sécurisée (hors Speech)** | | **~650–1 000 USD/mois** |

> **Variante sans exposition Internet** (accès uniquement via VPN /
> ExpressRoute, ingress Container Apps interne) : sans Front Door Premium, le
> socle descend à **~320–600 USD/mois** (hors Speech).
>
> **Options non incluses** : Azure DDoS Network Protection (~2 950 $/mois),
> Azure Firewall (~900 $/mois et plus), Microsoft Sentinel, Defender for Storage
> *malware scanning* (~0,15 $/Go analysé), multi-région / reprise d'activité.

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
| Fast Transcription, *pay-as-you-go* (actuel) | ~1,00 $/h | ~0,117 $ | **~48 600 $** | **~583 000 $** |
| Fast Transcription + *commitment tier* 50 000 h/mois¹ | ~0,50 $/h (forfait ~25 000 $/mois) | ~0,058 $ | **~25 000 $** | **~300 000 $** |
| Batch Transcription² | ~0,18 $/h | ~0,021 $ | **~8 750 $** | **~105 000 $** |

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

| Service | Hypothèse de charge | Coût approx. (USD/mois) |
| --- | --- | ---: |
| Azure Front Door Premium + WAF | Forfait + ~420 k uploads/mois (~2,9 To) + requêtes UI | ~400–650 |
| Azure Container Apps (frontend + API) | Plusieurs réplicas en heures ouvrées (uploads ~7 Mo) | ~150–300 |
| Azure Functions (Pro Code) | ~420 k exécutions/mois, chacune attend la réponse Speech (~30–90 s) | ~100–400 |
| Azure Cosmos DB | ~420 k jobs/mois (créations + mises à jour de statut + requêtes UI) | ~10–50 |
| Azure Blob Storage (2 comptes) | ~2,9 To écrits/mois, ~100–200 Go stockés, millions d'opérations | ~30–60 |
| Private Endpoints | ~9 endpoints + ~6–9 To de données traitées (0,01 $/Go) | ~130–165 |
| Log Analytics + Application Insights | ~50–150 Go/mois (traces par job), rétention 90 jours | ~150–400 |
| Microsoft Defender for Cloud | Inchangé (hors *malware scanning*) | ~40–100 |
| Autres (ACR Premium, NAT Gateway, Private DNS, Key Vault, Event Grid, purge) | Inchangé ; Event Grid ~1 M opérations/mois | ~90–100 |
| **Total socle production (hors Speech)** | | **~1 100–2 300 USD/mois** |

> Avec l'approche *Low Code* (Logic Apps Consumption, ~15–20 actions par
> fichier), compter **~400–800 $/mois** supplémentaires pour la transcription ;
> l'approche *Pro Code* (Functions) est recommandée à ce volume.
>
> Option : Defender for Storage *malware scanning* sur les uploads
> (~2,9 To × 0,15 $/Go) ≈ **+440 $/mois**.

### Total hypothèse centre d'appel

| Option Speech | Total / mois (USD) | Total / an (USD) | Coût complet par audio |
| --- | ---: | ---: | ---: |
| Fast Transcription *pay-as-you-go* | **~49 700–50 900** | **~596 000–611 000** | ~0,12 $ |
| Fast Transcription + engagement 50 000 h | **~26 100–27 300** | **~313 000–328 000** | ~0,065 $ |
| Batch Transcription | **~9 850–11 050** | **~118 000–133 000** | ~0,025 $ |

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

## Synthèse

| Volume audio / mois | Non-production | Production sécurisée |
| ---: | ---: | ---: |
| ~4 h (quotas par défaut) | ~10–20 $ | ~655–1 005 $ |
| ~40 h | ~45–55 $ | ~690–1 040 $ |
| ~730 h | ~735–745 $ | ~1 380–1 730 $ |
| 2 000 h | ~2 005–2 015 $ | ~2 650–3 000 $ (≈ 2 250–2 600 $ avec engagement Speech) |
| ~48 600 h (centre d'appel : 5 M audios/an × DMT 7 min) | — (volume de production) | ~49 700–50 900 $ (≈ 26 100–27 300 $ avec engagement 50 000 h ; ≈ 9 850–11 050 $ en Batch) |

- En **non-production**, le coût fixe est minime (~10 $/mois) : la facture suit
  presque uniquement le volume transcrit (~1 $ par heure d'audio).
- En **production sécurisée**, le coût est dominé par le **socle de sécurité**
  (Front Door/WAF, Private Endpoints, ACR Premium, Defender, logs) tant que le
  volume audio reste inférieur à ~700 h/mois ; au-delà, **Speech** devient le
  premier poste.
- Dans l'**hypothèse centre d'appel** (5 M audios/an, DMT 7 min), le budget
  annuel varie de **~600 k$** (*pay-as-you-go*) à **~320 k$** (engagement
  50 000 h) voire **~125 k$** (Batch Transcription).

## Leviers d'optimisation

- **Speech** : conserver les quotas `TranscriptionLimits` comme garde-fou,
  souscrire un *commitment tier* pour un volume régulier élevé, ou basculer les
  traitements non urgents en **Batch Transcription** (~0,18 $/h). Pour un
  centre d'appel (~48 600 h/mois), le palier d'engagement 50 000 h divise
  quasiment par deux la facture Speech, et le Batch la divise par ~5,5.
- **Non-production** : garder le scale-to-zero, la purge quotidienne et Defender
  désactivé ; supprimer l'environnement (`azd down`) lorsqu'il n'est pas utilisé.
- **Production** : mutualiser Front Door / Defender / Log Analytics avec
  d'autres applications, ajuster la rétention et l'échantillonnage
  d'Application Insights, et étudier les réservations (Container Apps,
  Cosmos DB) pour les charges stables.
- Mettre en place des **budgets et alertes Azure Cost Management** par
  environnement (groupe de ressources).

## Références

- Architecture détaillée : [architecture.md](architecture.md).
- Dossier projet et offre financière : [dossier-projet.md](dossier-projet.md).
- Vue d'ensemble et déploiement : [../README.md](../README.md).
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/).
- [Tarifs Azure AI Speech](https://azure.microsoft.com/pricing/details/cognitive-services/speech-services/).
- [Tarifs Azure Container Apps](https://azure.microsoft.com/pricing/details/container-apps/).
- [Tarifs Azure Front Door](https://azure.microsoft.com/pricing/details/frontdoor/).
- [Tarifs Private Link](https://azure.microsoft.com/pricing/details/private-link/).
- [Tarifs Microsoft Defender for Cloud](https://azure.microsoft.com/pricing/details/defender-for-cloud/).
