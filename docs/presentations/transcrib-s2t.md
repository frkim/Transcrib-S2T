---
marp: true
theme: transcrib
lang: fr
size: 16:9
paginate: true
title: Transcrib-S2T — Proposition de solution
description: Expression de besoins, solution technologique, plan projet et offre financière
author: Transcrib-S2T
footer: 'Transcrib-S2T · Proposition de solution · Chiffres indicatifs, hors taxes'
---

<!-- _class: lead -->
<!-- _paginate: false -->
<!-- _footer: '' -->

<span class="eyebrow">Proposition de solution · Octobre 2026</span>

# Transcrib-S2T

### Transformer **chaque conversation client** en levier de qualité

<div class="meta">Transcription automatique · Analyse qualité · Coaching des conseillers — sur Microsoft Azure<br/>Besoins · Solution · Plan projet · Offre financière</div>

---

# Vos enjeux : la qualité ne s'écoute pas à 1 %

<p class="kicker">Les enregistrements existent, mais leur valeur reste inexploitée.</p>

<div class="cols-4">
  <div class="kpi warn"><div class="v">1–2 <small>%</small></div><div class="l">des appels réellement écoutés par les équipes qualité</div></div>
  <div class="kpi"><div class="v">~15 <small>min</small></div><div class="l">par évaluation manuelle d'un appel de 7 minutes</div></div>
  <div class="kpi alt"><div class="v">5 <small>M</small></div><div class="l">d'appels par an dans l'hypothèse haute centre d'appel</div></div>
  <div class="kpi dark"><div class="v">100 <small>%</small></div><div class="l">de couverture visée, à coût unitaire maîtrisé</div></div>
</div>

<div class="cols-3" style="margin-top:26px">
  <div class="card red"><h3>Constat</h3><p>Échantillonnage manuel, chronophage et subjectif. Les signaux faibles (insatisfaction, non-conformité, risque de départ) passent inaperçus.</p></div>
  <div class="card amber"><h3>Contraintes</h3><p>Données personnelles sensibles : hébergement en France, conservation minimale, accès tracés. Budget prévisible.</p></div>
  <div class="card green"><h3>Ambition</h3><p>Transcrire tous les appels, objectiver l'évaluation et outiller un coaching fondé sur des preuves.</p></div>
</div>

---

# Expression de besoins

<div class="cols">
<div class="card">
  <h3>Besoins fonctionnels</h3>
  <ul>
    <li>Dépôt web d'un ou plusieurs MP3 <span class="pill ok">disponible</span></li>
    <li>Ingestion automatique depuis la téléphonie <span class="pill todo">à réaliser</span></li>
    <li>Transcription horodatée + identification des locuteurs <span class="pill ok">disponible</span></li>
    <li>Suivi des statuts et téléchargement <span class="pill ok">disponible</span></li>
    <li>Analyse qualité 7 étapes, comparaison, coaching 90 j, exports <span class="pill ok">disponible</span></li>
    <li>Purge automatique J+1, suppression à la demande <span class="pill ok">disponible</span></li>
    <li>Traitement Batch des gros volumes <span class="pill">option</span></li>
    <li>Analyse IA générative (Foundry) <span class="pill">option</span></li>
  </ul>
</div>
<div class="card teal">
  <h3>Exigences non fonctionnelles</h3>
  <ul>
    <li><strong>Sécurité</strong> : SSO Entra ID, Managed Identity, zéro secret</li>
    <li><strong>Conformité</strong> : France Central, conservation 24 h max.</li>
    <li><strong>Performance</strong> : appel de 7 min transcrit en &lt; 2 min</li>
    <li><strong>Disponibilité</strong> : 99,9 % en production</li>
    <li><strong>Scalabilité</strong> : serverless, pointe ~35 traitements/min</li>
    <li><strong>Observabilité</strong> : traces de bout en bout</li>
    <li><strong>Exploitabilité</strong> : IaC Bicep, CI/CD, runbooks</li>
    <li><strong>Accessibilité</strong> : WCAG 2.1 AA, thème clair/sombre</li>
  </ul>
</div>
</div>

---

# Notre réponse : un accélérateur prêt, à industrialiser

<p class="kicker">Transcrib-S2T fonctionne déjà de bout en bout : le projet sécurise et met à l'échelle, sans repartir d'une page blanche.</p>

<div class="cols-4">
  <div class="kpi"><div class="v">&lt; 2 <small>min</small></div><div class="l">pour transcrire un appel de 7 minutes (Fast Transcription)</div></div>
  <div class="kpi alt"><div class="v">100 <small>%</small></div><div class="l">des appels transcrits et analysables, plus d'échantillonnage</div></div>
  <div class="kpi warn"><div class="v">24 <small>h</small></div><div class="l">de conservation maximale : purge automatique J+1</div></div>
  <div class="kpi dark"><div class="v">0,023 <small>€</small></div><div class="l">coût complet par appel en mode Batch (option A)</div></div>
</div>

<div class="cols-3" style="margin-top:26px">
  <div class="card"><h3>Déjà opérationnel</h3><p>API, pipeline event-driven, frontend, analyse qualité V4.1, IaC Bicep, tests et CI.</p></div>
  <div class="card teal"><h3>Sécurisé par conception</h3><p>Managed Identity, Entra ID, analyse exécutée dans le navigateur, aucune donnée envoyée à un tiers.</p></div>
  <div class="card green"><h3>Payé à l'usage</h3><p>Services serverless et scale-to-zero : le coût suit le volume d'audio réellement traité.</p></div>
</div>

---

# Architecture cible — production sécurisée

<div class="diagram">
<svg viewBox="0 0 1160 470" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Architecture cible Transcrib-S2T">
  <defs>
    <marker id="ah" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
      <path d="M0,0 L10,5 L0,10 z" fill="#5b6577"/>
    </marker>
  </defs>
  <g font-size="13" fill="#5b6577" font-weight="600" letter-spacing="1">
    <rect x="0" y="0" width="150" height="380" rx="12" fill="#f3f7fc" stroke="#dbe3ee"/>
    <text x="75" y="24" text-anchor="middle">UTILISATEURS</text>
    <rect x="175" y="0" width="330" height="380" rx="12" fill="#f3f7fc" stroke="#dbe3ee"/>
    <text x="340" y="24" text-anchor="middle">EXPOSITION &amp; APPLICATION</text>
    <rect x="530" y="0" width="230" height="380" rx="12" fill="#f3f7fc" stroke="#dbe3ee"/>
    <text x="645" y="24" text-anchor="middle">DONNÉES · FRANCE CENTRAL</text>
    <rect x="785" y="0" width="375" height="380" rx="12" fill="#f3f7fc" stroke="#dbe3ee"/>
    <text x="972" y="24" text-anchor="middle">TRAITEMENT EVENT-DRIVEN</text>
  </g>
  <g fill="none" stroke="#5b6577" stroke-width="2" marker-end="url(#ah)">
    <path d="M138,105 C165,105 170,68 193,68"/>
    <path d="M138,255 H417 V229"/>
    <path d="M262,96 V138"/>
    <path d="M417,96 V138"/>
    <path d="M330,183 H348"/>
    <path d="M485,160 H515 V105 H548"/>
    <path d="M485,205 H515 V265 H548"/>
    <path d="M740,90 H803"/>
    <path d="M880,120 V188"/>
    <path d="M955,230 H988"/>
    <path d="M805,215 H780 V130 H742"/>
    <path d="M805,255 H742"/>
    <path d="M1065,60 V42 H645 V58" stroke-dasharray="6 5"/>
  </g>
  <g font-size="15" text-anchor="middle" fill="#0b1f3a">
    <rect x="12" y="70" width="126" height="70" rx="10" fill="#fff" stroke="#0078d4" stroke-width="2"/>
    <text x="75" y="100" font-weight="700">Superviseurs</text><text x="75" y="120" font-size="13" fill="#5b6577">coachs qualité</text>
    <rect x="12" y="220" width="126" height="70" rx="10" fill="#fff" stroke="#0078d4" stroke-width="2"/>
    <text x="75" y="250" font-weight="700">Téléphonie</text><text x="75" y="270" font-size="13" fill="#5b6577">SI client (API)</text>
    <rect x="195" y="40" width="290" height="56" rx="10" fill="#0b1f3a"/>
    <text x="340" y="66" font-weight="700" fill="#fff">Azure Front Door Premium + WAF</text>
    <text x="340" y="84" font-size="12" fill="#b8c8de">OWASP · bot protection · Private Link</text>
    <rect x="195" y="140" width="135" height="86" rx="10" fill="#fff" stroke="#0078d4" stroke-width="2"/>
    <text x="262" y="170" font-weight="700">Frontend</text><text x="262" y="190" font-size="13" fill="#5b6577">Next.js</text><text x="262" y="208" font-size="12" fill="#0078d4">Container Apps</text>
    <rect x="350" y="140" width="135" height="86" rx="10" fill="#fff" stroke="#0078d4" stroke-width="2"/>
    <text x="417" y="170" font-weight="700">API jobs</text><text x="417" y="190" font-size="13" fill="#5b6577">ASP.NET Core</text><text x="417" y="208" font-size="12" fill="#0078d4">Container Apps</text>
    <rect x="195" y="300" width="290" height="50" rx="10" fill="#fff" stroke="#00a3ad" stroke-width="2"/>
    <text x="340" y="330" font-weight="700">Microsoft Entra ID <tspan font-weight="400" fill="#5b6577">· SSO &amp; rôles</tspan></text>
    <rect x="550" y="60" width="190" height="90" rx="10" fill="#fff" stroke="#00a3ad" stroke-width="2"/>
    <text x="645" y="92" font-weight="700">Blob Storage</text><text x="645" y="112" font-size="13" fill="#5b6577">audio · transcripts</text><text x="645" y="132" font-size="12" fill="#00a3ad">purge J+1</text>
    <rect x="550" y="220" width="190" height="90" rx="10" fill="#fff" stroke="#00a3ad" stroke-width="2"/>
    <text x="645" y="252" font-weight="700">Cosmos DB</text><text x="645" y="272" font-size="13" fill="#5b6577">jobs &amp; statuts</text><text x="645" y="292" font-size="12" fill="#00a3ad">serverless</text>
    <rect x="805" y="60" width="150" height="60" rx="10" fill="#fff" stroke="#5b6577" stroke-width="2"/>
    <text x="880" y="86" font-weight="700">Event Grid</text><text x="880" y="105" font-size="12" fill="#5b6577">blob créé</text>
    <rect x="805" y="190" width="150" height="80" rx="10" fill="#fff" stroke="#0078d4" stroke-width="2"/>
    <text x="880" y="222" font-weight="700">Azure Functions</text><text x="880" y="242" font-size="13" fill="#5b6577">Pro Code</text><text x="880" y="259" font-size="12" fill="#0078d4">Flex Consumption</text>
    <rect x="990" y="180" width="150" height="100" rx="10" fill="#0078d4"/>
    <text x="1065" y="215" font-weight="700" fill="#fff">Azure AI Speech</text><text x="1065" y="236" font-size="13" fill="#e7f1fb">Fast · Batch</text><text x="1065" y="256" font-size="12" fill="#cfe3f7">diarization</text>
    <rect x="990" y="60" width="150" height="60" rx="10" fill="#fff" stroke="#5b6577" stroke-width="2" stroke-dasharray="5 4"/>
    <text x="1065" y="86" font-weight="700">Purge quotidienne</text><text x="1065" y="105" font-size="12" fill="#5b6577">cycle de vie</text>
  </g>
  <g font-size="12" font-weight="700" text-anchor="middle" fill="#0b1f3a">
    <circle cx="168" cy="84" r="11" fill="#f2a900"/><text x="168" y="88">1</text>
    <circle cx="300" cy="255" r="11" fill="#f2a900"/><text x="300" y="259">1</text>
    <circle cx="515" cy="132" r="11" fill="#f2a900"/><text x="515" y="136">2</text>
    <circle cx="760" cy="90" r="11" fill="#f2a900"/><text x="760" y="94">3</text>
    <circle cx="972" cy="212" r="11" fill="#f2a900"/><text x="972" y="216">4</text>
    <circle cx="762" cy="255" r="11" fill="#f2a900"/><text x="762" y="259">5</text>
    <circle cx="262" cy="118" r="11" fill="#f2a900"/><text x="262" y="122">6</text>
  </g>
  <rect x="0" y="400" width="1160" height="62" rx="12" fill="#0b1f3a"/>
  <text x="580" y="428" font-size="15" font-weight="700" fill="#fff" text-anchor="middle">Socle transverse</text>
  <text x="580" y="449" font-size="14" fill="#b8c8de" text-anchor="middle">Managed Identity (zéro secret) · VNet &amp; Private Endpoints · Application Insights · Defender for Cloud · IaC Bicep / azd · CI/CD GitHub Actions</text></svg>
</div>

<div class="legend">
  <span><b>1</b>Dépôt web ou API (téléphonie)</span>
  <span><b>2</b>Audio stocké, job « Processing »</span>
  <span><b>3</b>Événement « blob créé »</span>
  <span><b>4</b>Transcription + diarization</span>
  <span><b>5</b>Transcript et statut « Completed »</span>
  <span><b>6</b>Consultation et analyse qualité</span>
</div>

---

# Choix technologiques

| Brique | Choix | Pourquoi |
| --- | --- | --- |
| Transcription | **Azure AI Speech — Fast Transcription** | Synchrone, MP3 décodé côté service, diarization incluse, ~0,92 €/h d'audio |
| Gros volumes | **Speech Batch** (option A) | ~0,17 €/h : **−80 %** sur le premier poste de coût |
| Orchestration | **Azure Functions** + Event Grid | Event-driven, intégration VNet, coût marginal à fort volume |
| Application | **Next.js** + **ASP.NET Core** sur Container Apps | Scale-to-zero, conteneurs standard, zone redundancy |
| Données | **Blob Storage** + **Cosmos DB** serverless | Stockage éphémère (J+1), métadonnées payées à la requête |
| Identité | **Entra ID** + **Managed Identity** | SSO, RBAC fin, aucun secret à gérer ni à faire tourner |
| IA générative | **Microsoft Foundry** (option B) | Responses API, hébergement UE, gouvernance Azure |
| Industrialisation | **Bicep + azd**, **GitHub Actions** (OIDC) | Environnements reproductibles, déploiement sans secret |

<p class="small">Alternatives écartées : Speech SDK temps réel (décodage audio local), Logic Apps Consumption en production (incompatibles avec les Private Endpoints), App Service (coût fixe à faible charge).</p>

---

# Sécurité & conformité RGPD

<div class="cols">
  <div class="card"><h3>Identité et accès</h3><ul><li>SSO Microsoft Entra ID, rôles applicatifs</li><li>Managed Identity entre services : <strong>aucun secret</strong></li><li>Déploiement GitHub Actions par OIDC</li></ul></div>
  <div class="card teal"><h3>Réseau</h3><ul><li>VNet dédié, Private Endpoints sur toutes les données</li><li>Front Door Premium + WAF (OWASP, bots)</li><li>NAT Gateway : sortie maîtrisée</li></ul></div>
  <div class="card green"><h3>Données personnelles</h3><ul><li>Hébergement <strong>France Central</strong>, chiffrement au repos et en transit</li><li><strong>Purge automatique à J+1</strong>, suppression à la demande</li><li>Analyse qualité exécutée dans le navigateur</li></ul></div>
  <div class="card amber"><h3>Détection et preuve</h3><ul><li>Defender for Cloud (Storage, Containers, Cosmos DB)</li><li>Traces Application Insights, journaux WAF</li><li>AIPD et registre accompagnés avec votre DPO</li></ul></div>
</div>

---

# Plan projet estimatif — 7,5 semaines

<div class="gantt" style="--cols:18">
  <div class="lbl"></div>
  <div class="wk" style="grid-column:span 2">S1</div><div class="wk" style="grid-column:span 2">S2</div><div class="wk" style="grid-column:span 2">S3</div><div class="wk" style="grid-column:span 2">S4</div><div class="wk" style="grid-column:span 2">S5</div><div class="wk" style="grid-column:span 2">S6</div><div class="wk" style="grid-column:span 2">S7</div><div class="wk" style="grid-column:span 2">S8</div><div class="wk" style="grid-column:span 2">S9+</div>
  <div class="lbl" style="grid-column:1">P0 · Cadrage<span>besoins, architecture, AIPD</span></div>
  <div class="bar p0" style="grid-column:2 / 4">10 j·h</div>
  <div class="lbl" style="grid-column:1">P1 · Pilote<span>données réelles, mesure qualité</span></div>
  <div class="bar p1" style="grid-column:4 / 8">35 j·h · démonstrateur</div>
  <div class="lbl" style="grid-column:1">P2 · Industrialisation<span>sécurité, CI/CD, charge</span></div>
  <div class="bar p2" style="grid-column:8 / 14">61,5 j·h · production sécurisée</div>
  <div class="lbl" style="grid-column:1">P3 · Déploiement<span>bascule, formation, hypercare</span></div>
  <div class="bar p3" style="grid-column:14 / 17">18,5 j·h</div>
  <div class="lbl" style="grid-column:1">Run · MCO<span>support et évolutions</span></div>
  <div class="bar run" style="grid-column:18 / 20"></div>
  <div class="lbl" style="grid-column:1">Jalons</div>
  <div class="ms" style="grid-column:2"></div><div class="ms" style="grid-column:3"></div><div class="ms" style="grid-column:7"></div><div class="ms" style="grid-column:13"></div><div class="ms" style="grid-column:14"></div><div class="ms" style="grid-column:16"></div>
  <div class="lbl" style="grid-column:1"></div>
  <div class="mslbl" style="grid-column:2">J0<br/>Kick-off</div><div class="mslbl" style="grid-column:3">J1<br/>Archi.</div><div class="mslbl" style="grid-column:7">J2<br/>Go/No-Go</div><div class="mslbl" style="grid-column:13">J3<br/>Sécurité</div><div class="mslbl" style="grid-column:14">J4<br/>MEP</div><div class="mslbl" style="grid-column:16">J5<br/>VSR</div>
</div>

<div class="cols-4" style="margin-top:18px">
  <div class="card" style="border-top-color:#13325c"><h3>Cadrage</h3><p>Note de cadrage, architecture validée, backlog priorisé.</p></div>
  <div class="card"><h3>Pilote</h3><p>Mesure WER et diarization sur vos appels, grille calibrée.</p></div>
  <div class="card teal"><h3>Industrialisation</h3><p>IaC production, tests de charge et d'intrusion, runbooks.</p></div>
  <div class="card green"><h3>Déploiement</h3><p>Bascule progressive, formation, 1 semaine d'hypercare.</p></div>
</div>

---

# Organisation & gouvernance

<div class="cols-60">
<div>

| Profil | Rôle | j·h |
| --- | --- | ---: |
| Chef de projet | Pilotage, risques, comitologie | 17 |
| Architecte cloud Azure | Architecture, sécurité, revues | 14,5 |
| Développeurs seniors | Connecteur, API, frontend, tests | 42,5 |
| Ingénieur DevOps / sécurité | IaC, CI/CD, réseau, observabilité | 23 |
| Ingénieur QA | Recette, tests de charge | 16 |
| Consultant data / IA | Qualité de transcription, calibration | 12 |
| **Total** | | **125** |

</div>
<div>
  <div class="card" style="margin-bottom:12px"><h3>COPIL · mensuel + jalons</h3><p>Sponsor, relation client, DSI, RSSI, DPO. Arbitrages, budget, Go/No-Go.</p></div>
  <div class="card teal" style="margin-bottom:12px"><h3>COPROJ · hebdomadaire</h3><p>Avancement, risques, priorisation du backlog.</p></div>
  <div class="card green"><h3>Sprints d'une semaine</h3><p>Démonstration aux utilisateurs clés à chaque fin de sprint. Équipe de ~4 ETP au pic. <strong>Product owner client : ~1 j/semaine.</strong></p></div>
</div>
</div>

---

# Offre financière — Build au forfait

<div class="cols">
<div>

| Phase | j·h | € HT |
| --- | ---: | ---: |
| P0 · Cadrage | 10 | 10 600 |
| P1 · Pilote | 35 | 31 750 |
| P2 · Industrialisation | 61,5 | 55 575 |
| P3 · Déploiement & adoption | 18,5 | 16 800 |
| **Total forfait** | **125** | **114 725** |

<div class="callout"><strong>Engagement par paliers</strong> : P0 + P1 commandables seuls (<strong>42 350 € HT</strong>). La suite est conditionnée au Go/No-Go de S3.</div>

</div>
<div>
  <h2>Échéancier de facturation</h2>
  <div class="bars">
    <div>Commande (J0)</div><div class="track"><div class="fill navy" style="width:57%"></div></div><div class="val">20 %</div>
    <div>Go/No-Go pilote (J2)</div><div class="track"><div class="fill" style="width:71%"></div></div><div class="val">25 %</div>
    <div>Mise en production (J4)</div><div class="track"><div class="fill teal" style="width:100%"></div></div><div class="val">35 %</div>
    <div>VSR (J5)</div><div class="track"><div class="fill green" style="width:57%"></div></div><div class="val">20 %</div>
  </div>
  <p class="small" style="margin-top:18px">TJM : chef de projet 950 € · architecte 1 200 € · développeur 850 € · DevOps 950 € · QA 700 € · data/IA 1 000 € (TJM moyen ≈ 918 €). Offre valable 3 mois, hors frais de déplacement.</p>
</div>
</div>

---

# Coûts de fonctionnement & options

<div class="cols">
<div>
  <h2>Run mensuel</h2>

| Poste | Coût / mois |
| --- | ---: |
| MCO / TMA (3 j·h) | 2 700 € HT |
| Azure non-production | ~7–18 € |
| Azure production sécurisée (hors Speech) | ~600–920 € |
| Speech Fast Transcription | ~0,92 € / h d'audio |
| Speech Batch (option A) | ~0,17 € / h d'audio |

<p class="small">Consommation Azure facturée par Microsoft sur votre abonnement. Montants HT, région France Central par défaut (alternatives : Sweden Central, puis North Europe). Détail : docs/pricing.md.</p>
</div>
<div>
  <h2>Options</h2>
  <div class="card teal" style="margin-bottom:12px"><h3>A · Batch Transcription — 25 100 € HT</h3><p>Traitement différé des gros volumes : −80 % sur le coût Speech. Rentabilisé en moins d'un mois à 5 M d'appels/an.</p></div>
  <div class="card" style="margin-bottom:12px"><h3>B · Analyse IA générative — 37 100 € HT</h3><p>Synthèse, motifs d'appel et intentions via Microsoft Foundry (Responses API).</p></div>
  <div class="card amber"><h3>C · Connecteur téléphonie avancé — 16 050 € HT</h3><p>Collecte planifiée depuis la plateforme d'enregistrement, métadonnées d'appel.</p></div>
</div>
</div>

---

# Valeur & retour sur investissement

<div class="cols-4">
  <div class="kpi"><div class="v">×100</div><div class="l">couverture qualité : de 1 % à 100 % des appels</div></div>
  <div class="kpi alt"><div class="v">~8 300 <small>h</small></div><div class="l">de superviseur libérées par an (15 → 5 min par évaluation)</div></div>
  <div class="kpi warn"><div class="v">~375 <small>k€</small></div><div class="l">de gain de productivité annuel (45 €/h chargé)</div></div>
  <div class="kpi dark"><div class="v">7–8 <small>mois</small></div><div class="l">de retour sur investissement (build + option A)</div></div>
</div>

<h2 style="margin-top:26px">Coût Azure annuel — 5 M d'appels × 7 min (~48 600 h d'audio / mois)</h2>
<div class="bars">
  <div>Fast, pay-as-you-go</div><div class="track"><div class="fill red" style="width:100%"></div></div><div class="val">~555 k€</div>
  <div>Fast + engagement 50 000 h</div><div class="track"><div class="fill amber" style="width:53%"></div></div><div class="val">~295 k€</div>
  <div>Batch (option A)</div><div class="track"><div class="fill green" style="width:21%"></div></div><div class="val">~115 k€</div>
</div>

<p class="small" style="margin-top:14px">Hypothèses : 1 % des appels évalués aujourd'hui (50 000/an) ; run annuel Batch + MCO ≈ 141–154 k€ ; investissement 139 825 € HT (build 114 725 + option A 25 100). Gain net ≈ 221–234 k€/an. À affiner au cadrage.</p>

---

# Variante budgétaire : analyse échantillonnée

<p class="kicker">Si la couverture exhaustive n'est pas requise, ne transcrire qu'un échantillon des appels réduit la facture Speech d'autant.</p>

<div class="cols">
<div>
  <h2>Coût Azure annuel selon le taux</h2>

| Taux | Fast, pay-as-you-go | Batch (option A) |
| ---: | ---: | ---: |
| 5 % | ~34–39 k€ | ~12–17 k€ |
| 10 % | ~61–66 k€ | ~17–22 k€ |
| **15 %** | **~88–94 k€** | **~22–28 k€** |
| 25 % | ~143–149 k€ | ~33–39 k€ |
| 50 % | ~278–286 k€ | ~58–66 k€ |
| 100 % (référence) | ~548–562 k€ | ~109–122 k€ |

<p class="small">Production sécurisée, 5 M d'appels/an × 7 min, échantillonnage à la source (téléphonie). Détail : docs/pricing.md.</p>
</div>
<div>
  <h2>Quel taux retenir ?</h2>
  <div class="card green" style="margin-bottom:12px"><h3>Pilotage global — 5 à 10 %</h3><p>Tendances, motifs d'appel, irritants : marge d'erreur inférieure à ±1 point.</p></div>
  <div class="card teal" style="margin-bottom:12px"><h3>Coaching individuel — 15 à 25 %</h3><p>Ou un quota fixe de ~100–150 appels par conseiller et par mois (échantillonnage stratifié).</p></div>
  <div class="card amber"><h3>Exhaustif — 100 %</h3><p>Conformité, litiges, détection systématique des signaux faibles.</p></div>
</div>
</div>

<div class="callout">À <strong>15 % en Batch</strong>, retour sur investissement en <strong>~5 mois</strong> (au lieu de 7–8), gain de productivité préservé. En contrepartie, la couverture à 100 % est abandonnée : option budgétaire ou phase de montée en charge.</div>

---

# Risques maîtrisés

| Risque | Niveau | Mitigation |
| --- | :---: | --- |
| Qualité de transcription (audio 8 kHz, bruit, accents) | <span class="pill hi">élevé</span> | Mesure du WER dès le pilote sur vos appels, Custom Speech si besoin, Go/No-Go S3 |
| Données personnelles sensibles | <span class="pill hi">élevé</span> | AIPD au cadrage, purge J+1, réseau privé, accès Entra ID tracés |
| Quotas Azure AI Speech en pointe | <span class="pill todo">moyen</span> | Tests de charge (pointe ×2), demande de quotas anticipée, reprise sur erreur |
| Dérive des coûts de consommation | <span class="pill todo">moyen</span> | Quotas applicatifs, budgets et alertes Cost Management, option Batch |
| Adoption par les superviseurs | <span class="pill todo">moyen</span> | Utilisateurs clés en revue de sprint, formation, grille calibrée sur vos pratiques |
| Logic Apps incompatibles avec le réseau privé | <span class="pill ok">faible</span> | Pipeline Pro Code (Functions) et cycle de vie Blob en production |

---

<!-- _class: lead -->
<!-- _paginate: false -->
<!-- _footer: '' -->

<span class="eyebrow">Prochaines étapes</span>

# Démarrons par le pilote

### Une décision à risque maîtrisé : **42 350 € HT** pour prouver la valeur sur vos données en **3 semaines**

<div class="meta">
1 · Atelier de restitution avec le sponsor et la DSI (1 h 30)<br/>
2 · Mise à disposition d'un échantillon d'enregistrements et d'un accès Azure<br/>
3 · Commande P0 + P1, lancement sous 2 semaines<br/>
4 · Go/No-Go en semaine 3 sur indicateurs mesurés
</div>
