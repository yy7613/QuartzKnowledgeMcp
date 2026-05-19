const summaryEndpoint = document.body.dataset.summaryEndpoint ?? "/api/dashboard/summary?recentPerStage=8";
const searchEndpoint = document.body.dataset.searchEndpoint ?? "/api/dashboard/search";
const dashboardStateStorageKey = "quartz-knowledge-dashboard-state-v4";

const overviewCards = document.getElementById("overview-cards");
const stageColumns = document.getElementById("stage-columns");
const tagCloud = document.getElementById("tag-cloud");
const tagMeta = document.getElementById("tag-meta");
const searchForm = document.getElementById("dashboard-search-form");
const searchQuery = document.getElementById("search-query");
const searchResults = document.getElementById("search-results");
const searchMeta = document.getElementById("search-meta");
const searchStatus = document.getElementById("search-status");
const refreshSummaryButton = document.getElementById("refresh-summary");
const stageFilter = document.getElementById("stage-filter");
const freshnessFilter = document.getElementById("freshness-filter");
const searchSort = document.getElementById("search-sort");
const clearSearchFiltersButton = document.getElementById("clear-search-filters");
const trendWindowToggle = document.getElementById("trend-window-toggle");
const dashboardTabList = document.getElementById("dashboard-tablist");
const dashboardTabs = Array.from(document.querySelectorAll("[data-tab]"));
const dashboardPanels = Array.from(document.querySelectorAll("[data-dashboard-panel]"));
const inspectorMeta = document.getElementById("inspector-meta");
const clearInspectorButton = document.getElementById("clear-inspector");
const inspectorEmpty = document.getElementById("gold-inspector-empty");
const inspectorPanel = document.getElementById("gold-inspector-panel");
const resultPreviewDialog = document.getElementById("result-preview-dialog");
const resultPreviewMeta = document.getElementById("result-preview-meta");
const resultPreviewBody = document.getElementById("result-preview-body");
const resultPreviewCloseButton = document.getElementById("result-preview-close");

let selectedStage = "all";
let selectedTag = "";
let trendWindow = "7d";
let selectedInspectorId = "";
let activeDashboardTab = "search";
let currentSummary = null;

refreshSummaryButton.addEventListener("click", () => {
  void loadSummary();
});

searchQuery.addEventListener("input", () => {
  updateSearchStatus();
});

stageFilter.addEventListener("click", (event) => {
  const target = event.target;
  if (!(target instanceof HTMLButtonElement)) {
    return;
  }

  setSelectedStage(target.dataset.stage ?? "all");
  updateSearchStatus();
});

freshnessFilter.addEventListener("change", () => {
  updateSearchStatus();
});

searchSort.addEventListener("change", () => {
  updateSearchStatus();
});

trendWindowToggle.addEventListener("click", (event) => {
  const target = event.target;
  if (!(target instanceof HTMLButtonElement)) {
    return;
  }

  setTrendWindow(target.dataset.window ?? "7d");
  setActiveDashboardTab("analytics");
  if (currentSummary) {
    renderStages(currentSummary);
  }
  persistDashboardState();
});

dashboardTabList.addEventListener("click", (event) => {
  const target = event.target;
  if (!(target instanceof HTMLButtonElement)) {
    return;
  }

  setActiveDashboardTab(target.dataset.tab ?? "search");
  persistDashboardState();
});

clearSearchFiltersButton.addEventListener("click", () => {
  selectedTag = "";
  setSelectedStage("all");
  freshnessFilter.value = "all";
  searchSort.value = "newest";
  updateSearchStatus();
  if (currentSummary) {
    renderTags(currentSummary.tags);
  }

  if (!searchQuery.value.trim()) {
    searchMeta.textContent = "キーワードを入力して検索を開始してください。";
    searchResults.className = "search-results empty-state";
    searchResults.textContent = "キーワードを入力して検索を開始してください。";
  }
});

clearInspectorButton.addEventListener("click", () => {
  clearInspector();
});

if (resultPreviewCloseButton instanceof HTMLButtonElement) {
  resultPreviewCloseButton.addEventListener("click", () => {
    closeResultPreview();
  });
}

if (resultPreviewDialog instanceof HTMLDialogElement) {
  resultPreviewDialog.addEventListener("click", (event) => {
    if (event.target === resultPreviewDialog) {
      closeResultPreview();
    }
  });
}

searchForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  await runSearch();
});

void initializeDashboard();

async function initializeDashboard() {
  restoreDashboardState();
  updateSearchStatus();
  await loadSummary();

  if (hasSearchIntent()) {
    await runSearch();
  }

  if (selectedInspectorId) {
    await inspectGoldEntry(selectedInspectorId);
  }
}

async function loadSummary() {
  setLoadingState(overviewCards, "summary を読み込み中...");
  setLoadingState(stageColumns, "medallion 情報を読み込み中...");
  setLoadingState(tagCloud, "タグ一覧を読み込み中...");

  try {
    const response = await fetch(summaryEndpoint, { headers: { Accept: "application/json" } });
    if (!response.ok) {
      throw new Error(`summary request failed: ${response.status}`);
    }

    const summary = await response.json();
    currentSummary = summary;
    renderOverview(summary);
    renderStages(summary);
    renderTags(summary.tags);
  } catch (error) {
    currentSummary = null;
    renderError(overviewCards, error);
    renderError(stageColumns, error);
    renderError(tagCloud, error);
  }
}

async function runSearch() {
  const query = searchQuery.value.trim();
  const hasFilters = Boolean(selectedTag) || selectedStage !== "all" || freshnessFilter.value !== "all";

  if (!query && !hasFilters) {
    searchMeta.textContent = "キーワードを入力して検索を開始してください。";
    searchResults.className = "search-results empty-state";
    searchResults.textContent = "キーワードを入力して検索を開始してください。";
    return;
  }

  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }
  if (selectedStage !== "all") {
    params.set("stage", selectedStage);
  }
  if (selectedTag) {
    params.set("tag", selectedTag);
  }
  if (freshnessFilter.value !== "all") {
    params.set("freshness", freshnessFilter.value);
  }
  params.set("sort", searchSort.value);
  params.set("limit", "24");

  setActiveDashboardTab("search");
  searchMeta.textContent = `検索中: ${query || "filter browse"}`;
  updateSearchStatus();
  setLoadingState(searchResults, "検索結果を読み込み中...");

  try {
    const response = await fetch(`${searchEndpoint}?${params.toString()}`, {
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      throw new Error(`search request failed: ${response.status}`);
    }

    const payload = await response.json();
    renderSearchResults(payload);
    if (currentSummary) {
      renderTags(currentSummary.tags);
    }
  } catch (error) {
    renderError(searchResults, error);
  }
}

function renderOverview(summary) {
  overviewCards.className = "overview-grid";
  overviewCards.replaceChildren();

  const cards = [
    { label: "Bronze", value: summary.bronze.totalCount, hint: freshnessHint(summary.bronze.freshness) },
    { label: "Silver", value: summary.silver.totalCount, hint: freshnessHint(summary.silver.freshness) },
    { label: "Gold", value: summary.gold.totalCount, hint: freshnessHint(summary.gold.freshness) },
    { label: "Tags", value: summary.tags.uniqueCount, hint: `最終更新 ${formatTimestamp(summary.overview.latestActivityAtUtc)}` }
  ];

  const template = document.getElementById("overview-card-template");
  for (const [index, card] of cards.entries()) {
    const node = template.content.firstElementChild.cloneNode(true);
    node.style.animationDelay = `${index * 50}ms`;
    node.querySelector(".metric-card__label").textContent = card.label;
    node.querySelector(".metric-card__value").textContent = String(card.value);
    node.querySelector(".metric-card__hint").textContent = card.hint;
    overviewCards.append(node);
  }
}

function renderStages(summary) {
  stageColumns.className = "stage-columns";
  stageColumns.replaceChildren();

  const template = document.getElementById("stage-card-template");
  const stages = [summary.bronze, summary.silver, summary.gold];

  for (const [index, stage] of stages.entries()) {
    const node = template.content.firstElementChild.cloneNode(true);
    node.style.animationDelay = `${index * 70}ms`;
    node.querySelector(".stage-card__name").textContent = stage.stage;
    node.querySelector(".stage-card__count").textContent = `${stage.totalCount} 件`;
    node.querySelector(".stage-card__latest").textContent = `最新: ${formatTimestamp(stage.latestActivityAtUtc)}`;

    const trendStrip = node.querySelector(".trend-strip");
    const allTrendPoints = Array.isArray(stage.trend) ? stage.trend : [];
    const trendPoints = trendWindow === "3d" ? allTrendPoints.slice(-3) : allTrendPoints;
    trendStrip.style.gridTemplateColumns = `repeat(${Math.max(trendPoints.length, 1)}, minmax(0, 1fr))`;
    const maxTrendCount = Math.max(...trendPoints.map((point) => point.count), 1);
    for (const point of trendPoints) {
      const bar = document.createElement("div");
      bar.className = "trend-bar";
      const fillHeight = Math.max(22, Math.round((point.count / maxTrendCount) * 72));
      bar.innerHTML = `
        <div class="trend-bar__fill" style="height:${fillHeight}px"></div>
        <span class="trend-bar__count">${point.count}</span>
        <span class="trend-bar__label">${point.day.slice(5)}</span>`;
      trendStrip.append(bar);
    }

    const freshnessGrid = node.querySelector(".freshness-grid");
    const freshnessItems = [
      ["24h", stage.freshness.last24Hours],
      ["7d", stage.freshness.last7Days],
      ["older", stage.freshness.older]
    ];

    for (const [label, count] of freshnessItems) {
      const item = document.createElement("div");
      item.className = "freshness-item";
      item.innerHTML = `<span>${label}</span><strong>${count}</strong>`;
      freshnessGrid.append(item);
    }

    const breakdownList = node.querySelector(".breakdown-list");
    if (stage.breakdown.length === 0) {
      const placeholder = document.createElement("div");
      placeholder.className = "breakdown-item";
      placeholder.innerHTML = "<span>補助集計</span><strong>なし</strong>";
      breakdownList.append(placeholder);
    } else {
      for (const breakdown of stage.breakdown.slice(0, 6)) {
        const item = document.createElement("div");
        item.className = "breakdown-item";
        item.innerHTML = `<span>${escapeHtml(breakdown.label)}</span><strong>${breakdown.count}</strong>`;
        breakdownList.append(item);
      }
    }

    const recentList = node.querySelector(".recent-list");
    for (const recent of stage.recentItems.slice(0, 5)) {
      const item = document.createElement("div");
      item.className = "recent-item";
      const inspectorAction = stage.stage === "gold"
        ? '<button class="button button--ghost button--compact recent-item__inspect" type="button">Inspect</button>'
        : "";
      item.innerHTML = `
        <div class="recent-item__body">
          <p class="recent-item__title"><a class="recent-item__link" href="${escapeAttribute(recent.detailPath)}" target="_blank" rel="noreferrer">${escapeHtml(recent.title)}</a></p>
          <p class="recent-item__subtitle">${escapeHtml(recent.subtitle)}</p>
        </div>
        <div class="recent-item__aside">
          <p class="recent-item__title">${escapeHtml(recent.state)}</p>
          <p class="recent-item__subtitle">${formatTimestamp(recent.timestampUtc)}</p>
          ${inspectorAction}
        </div>`;
      const inspectButton = item.querySelector(".recent-item__inspect");
      if (inspectButton instanceof HTMLButtonElement) {
        inspectButton.addEventListener("click", async () => {
          await inspectGoldEntry(recent.id);
        });
      }
      recentList.append(item);
    }

    stageColumns.append(node);
  }
}

function renderTags(tags) {
  tagCloud.className = "tag-cloud";
  tagCloud.replaceChildren();
  tagMeta.textContent = `${tags.uniqueCount} 個のユニークタグを頻度順に表示しています。${selectedTag ? ` / active: ${selectedTag}` : ""}`;

  if (tags.items.length === 0) {
    tagCloud.className = "tag-cloud empty-state";
    tagCloud.textContent = "タグはまだありません。";
    return;
  }

  for (const [index, tag] of tags.items.slice(0, 40).entries()) {
    const item = document.createElement("button");
    item.type = "button";
    item.className = `tag-chip reveal${selectedTag === tag.label ? " tag-chip--active" : ""}`;
    item.style.animationDelay = `${index * 20}ms`;
    item.textContent = `${tag.label} (${tag.count})`;
    item.addEventListener("click", async () => {
      selectedTag = selectedTag === tag.label ? "" : tag.label;
      setActiveDashboardTab("search");
      updateSearchStatus();
      renderTags(tags);
      await runSearch();
    });
    tagCloud.append(item);
  }
}

function renderSearchResults(payload) {
  searchMeta.textContent = `${payload.items.length}/${payload.totalCount} 件表示${payload.stage ? ` / stage=${payload.stage}` : ""}${payload.tag ? ` / tag=${payload.tag}` : ""}`;
  searchResults.className = "search-results";
  searchResults.replaceChildren();
  updateSearchStatus(payload);

  if (payload.items.length === 0) {
    searchResults.className = "search-results empty-state";
    searchResults.textContent = "一致する結果がありません。";
    return;
  }

  const template = document.getElementById("search-item-template");
  for (const [index, item] of payload.items.entries()) {
    const node = template.content.firstElementChild.cloneNode(true);
    node.style.animationDelay = `${index * 28}ms`;
    const stagePill = node.querySelector(".stage-pill");
    stagePill.dataset.stage = item.stage;
    stagePill.textContent = item.stage;
    node.querySelector(".result-card__time").textContent = formatTimestamp(item.timestampUtc);
    const link = node.querySelector(".result-card__link");
    link.href = item.detailPath;
    link.textContent = item.title;
    link.addEventListener("click", async (event) => {
      event.preventDefault();
      await openResultPreview(item);
    });
    const inspectButton = node.querySelector(".result-card__inspect");
    inspectButton.hidden = item.stage !== "gold";
    if (item.stage === "gold") {
      inspectButton.addEventListener("click", async () => {
        await inspectGoldEntry(item.id);
      });
    }
    node.querySelector(".result-card__summary").textContent = item.summary;
    node.querySelector(".result-card__state").textContent = `状態: ${item.state} / freshness: ${item.freshnessBucket}`;

    const tags = node.querySelector(".result-card__tags");
    for (const tag of item.tags.slice(0, 6)) {
      const chip = document.createElement("button");
      chip.type = "button";
      chip.className = `tag-chip${selectedTag === tag ? " tag-chip--active" : ""}`;
      chip.textContent = tag;
      chip.addEventListener("click", async () => {
        selectedTag = tag;
        setActiveDashboardTab("search");
        updateSearchStatus(payload);
        if (currentSummary) {
          renderTags(currentSummary.tags);
        }
        await runSearch();
      });
      tags.append(chip);
    }

    searchResults.append(node);
  }
}

async function openResultPreview(item) {
  if (!(resultPreviewDialog instanceof HTMLDialogElement) || !resultPreviewMeta || !resultPreviewBody) {
    window.open(item.detailPath, "_blank", "noreferrer");
    return;
  }

  resultPreviewMeta.textContent = `${item.stage} detail を読み込み中...`;
  resultPreviewBody.className = "preview-dialog__body empty-state";
  resultPreviewBody.textContent = "詳細を読み込み中...";

  if (!resultPreviewDialog.open) {
    resultPreviewDialog.showModal();
  }

  try {
    const response = await fetch(item.detailPath, { headers: { Accept: "application/json" } });
    if (!response.ok) {
      throw new Error(`preview request failed: ${response.status}`);
    }

    const payload = await response.json();
    renderResultPreview(item, payload);
    resultPreviewMeta.textContent = `${item.title} / ${item.stage} / ${formatTimestamp(item.timestampUtc)}`;
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    resultPreviewBody.className = "preview-dialog__body empty-state";
    resultPreviewBody.textContent = `読み込みに失敗しました: ${message}`;
    resultPreviewMeta.textContent = `preview error: ${message}`;
  }
}

function closeResultPreview() {
  if (resultPreviewDialog instanceof HTMLDialogElement && resultPreviewDialog.open) {
    resultPreviewDialog.close();
  }
}

function renderResultPreview(item, payload) {
  if (!resultPreviewBody) {
    return;
  }

  resultPreviewBody.className = "preview-dialog__body";

  switch (item.stage) {
    case "bronze":
      renderBronzePreview(item, payload);
      break;
    case "silver":
      renderSilverPreview(item, payload);
      break;
    case "gold":
      renderGoldPreview(item, payload);
      break;
    default:
      resultPreviewBody.innerHTML = '<div class="empty-state">未対応の stage です。</div>';
      break;
  }
}

function renderBronzePreview(item, detail) {
  const rawContent = truncateText(detail.rawContent || "", 3600);
  resultPreviewBody.innerHTML = `
    <section class="preview-dialog__section reveal">
      <div class="preview-dialog__summary-topline">
        <div>
          <p class="section-kicker">Bronze</p>
          <h3 class="preview-dialog__summary-title">${escapeHtml(detail.sourceUri || item.title)}</h3>
        </div>
        <div class="preview-dialog__actions">
          <a class="button button--ghost button--compact" href="${escapeAttribute(item.detailPath)}" target="_blank" rel="noreferrer">JSON</a>
        </div>
      </div>
      <p class="preview-dialog__summary-text">${escapeHtml(item.summary || "raw content preview")}</p>
      <div class="preview-dialog__meta-grid">
        ${renderPreviewMetaItem("Source type", detail.sourceType)}
        ${renderPreviewMetaItem("Status", detail.status)}
        ${renderPreviewMetaItem("Imported", formatTimestamp(detail.importedAtUtc))}
        ${renderPreviewMetaItem("Imported by", detail.importedBy || "なし")}
      </div>
    </section>
    <section class="preview-dialog__section reveal">
      <p class="section-kicker">Raw Content</p>
      <pre class="preview-dialog__pre">${escapeHtml(rawContent || "内容なし")}</pre>
    </section>`;
}

function renderSilverPreview(item, detail) {
  const tagsMarkup = renderInlineTags(detail.tagCandidates || []);
  const toolMarkup = (detail.toolDrafts || []).length === 0
    ? '<div class="preview-dialog__list-item"><strong>Tools</strong><span>tool draft はありません。</span></div>'
    : detail.toolDrafts.map((tool) => `
      <div class="preview-dialog__list-item">
        <strong>${escapeHtml(tool.name)}</strong>
        <span>${escapeHtml(tool.description)}</span>
      </div>`).join("");

  resultPreviewBody.innerHTML = `
    <section class="preview-dialog__section reveal">
      <div class="preview-dialog__summary-topline">
        <div>
          <p class="section-kicker">Silver</p>
          <h3 class="preview-dialog__summary-title">${escapeHtml(detail.name || item.title)}</h3>
        </div>
        <div class="preview-dialog__actions">
          <a class="button button--ghost button--compact" href="${escapeAttribute(item.detailPath)}" target="_blank" rel="noreferrer">JSON</a>
        </div>
      </div>
      <p class="preview-dialog__summary-text">${escapeHtml(detail.summary || item.summary)}</p>
      <div class="preview-dialog__meta-grid">
        ${renderPreviewMetaItem("Organized", formatTimestamp(detail.organizedAtUtc))}
        ${renderPreviewMetaItem("Bronze source", detail.bronzeSourceId || "なし")}
      </div>
      <div class="tag-cloud">${tagsMarkup}</div>
    </section>
    <section class="preview-dialog__section reveal">
      <p class="section-kicker">Tool Drafts</p>
      <div class="preview-dialog__list">${toolMarkup}</div>
    </section>`;
}

function renderGoldPreview(item, detail) {
  const tagsMarkup = renderInlineTags(detail.tags || []);
  const referencesMarkup = (detail.references || []).length === 0
    ? '<div class="preview-dialog__list-item"><strong>References</strong><span>reference はありません。</span></div>'
    : detail.references.map((reference) => `
      <div class="preview-dialog__list-item">
        <strong>${escapeHtml(reference.label)}</strong>
        <span><a class="inspector-anchor" href="${escapeAttribute(reference.url)}" target="_blank" rel="noreferrer">${escapeHtml(reference.url)}</a></span>
      </div>`).join("");
  const toolMarkup = (detail.toolSummaries || []).length === 0
    ? '<div class="preview-dialog__list-item"><strong>Tools</strong><span>tool summary はありません。</span></div>'
    : detail.toolSummaries.map((tool) => `
      <div class="preview-dialog__list-item">
        <strong>${escapeHtml(tool.name)}</strong>
        <span>${escapeHtml(tool.description)}</span>
      </div>`).join("");

  resultPreviewBody.innerHTML = `
    <section class="preview-dialog__section reveal">
      <div class="preview-dialog__summary-topline">
        <div>
          <p class="section-kicker">Gold</p>
          <h3 class="preview-dialog__summary-title">${escapeHtml(detail.displayName || item.title)}</h3>
        </div>
        <div class="preview-dialog__actions">
          <button class="button button--ghost button--compact" type="button" id="preview-open-inspector">Inspector で開く</button>
          <a class="button button--ghost button--compact" href="${escapeAttribute(item.detailPath)}" target="_blank" rel="noreferrer">JSON</a>
        </div>
      </div>
      <p class="preview-dialog__summary-text">${escapeHtml(detail.overview || item.summary)}</p>
      <div class="tag-cloud">${tagsMarkup}</div>
      <div class="preview-dialog__meta-grid">
        ${renderPreviewMetaItem("Auth", detail.authenticationType || "unknown")}
        ${renderPreviewMetaItem("Clients", (detail.supportedClients || []).join(", ") || "なし")}
        ${renderPreviewMetaItem("Published", formatTimestamp(detail.publishedAtUtc))}
        ${renderPreviewMetaItem("Updated", formatTimestamp(detail.updatedAtUtc))}
      </div>
    </section>
    <section class="preview-dialog__grid reveal">
      <article class="preview-dialog__section">
        <p class="section-kicker">Setup</p>
        <pre class="preview-dialog__pre">${escapeHtml(detail.setupGuide || "なし")}</pre>
      </article>
      <article class="preview-dialog__section">
        <p class="section-kicker">References</p>
        <div class="preview-dialog__list">${referencesMarkup}</div>
      </article>
    </section>
    <section class="preview-dialog__section reveal">
      <p class="section-kicker">Tool Summaries</p>
      <div class="preview-dialog__list">${toolMarkup}</div>
    </section>`;

  const inspectButton = resultPreviewBody.querySelector("#preview-open-inspector");
  if (inspectButton instanceof HTMLButtonElement) {
    inspectButton.addEventListener("click", async () => {
      closeResultPreview();
      await inspectGoldEntry(detail.id);
    });
  }
}

function renderPreviewMetaItem(label, value) {
  return `
    <div class="preview-dialog__meta-item">
      <strong>${escapeHtml(label)}</strong>
      <span>${escapeHtml(value)}</span>
    </div>`;
}

function renderInlineTags(tags) {
  if (!Array.isArray(tags) || tags.length === 0) {
    return '<span class="tag-chip">tag なし</span>';
  }

  return tags.map((tag) => `<span class="tag-chip">${escapeHtml(tag)}</span>`).join("");
}

function truncateText(value, maxLength) {
  if (!value || value.length <= maxLength) {
    return value;
  }

  return `${value.slice(0, maxLength)}\n\n...`;
}

function setSelectedStage(stage) {
  selectedStage = stage;
  for (const button of stageFilter.querySelectorAll("button")) {
    const isActive = button.dataset.stage === stage;
    button.classList.toggle("chip--active", isActive);
    button.setAttribute("aria-pressed", isActive ? "true" : "false");
  }
}

function setTrendWindow(windowValue) {
  trendWindow = normalizeTrendWindow(windowValue);
  for (const button of trendWindowToggle.querySelectorAll("button")) {
    const isActive = button.dataset.window === trendWindow;
    button.classList.toggle("chip--active", isActive);
    button.setAttribute("aria-pressed", isActive ? "true" : "false");
  }
}

function setActiveDashboardTab(tabValue) {
  activeDashboardTab = normalizeTab(tabValue);

  for (const tab of dashboardTabs) {
    const isActive = tab.dataset.tab === activeDashboardTab;
    tab.classList.toggle("tab-chip--active", isActive);
    tab.setAttribute("aria-selected", isActive ? "true" : "false");
  }

  for (const panel of dashboardPanels) {
    panel.hidden = panel.dataset.dashboardPanel !== activeDashboardTab;
  }
}

function updateSearchStatus(payload) {
  const effectiveQuery = payload && typeof payload.query === "string" ? payload.query : searchQuery.value.trim();
  const effectiveTag = payload && typeof payload.tag === "string" ? payload.tag : selectedTag;
  const effectiveFreshness = payload && typeof payload.freshness === "string" ? payload.freshness : freshnessFilter.value;
  const effectiveSort = payload && typeof payload.sort === "string" ? payload.sort : searchSort.value;
  const effectiveStage = payload && typeof payload.stage === "string" ? payload.stage : selectedStage;

  const parts = [
    effectiveQuery ? `query ${effectiveQuery}` : "query なし",
    effectiveTag ? `tag ${effectiveTag}` : "tag なし",
    `freshness ${effectiveFreshness}`,
    `sort ${effectiveSort}`,
    `stage ${effectiveStage}`,
    `trend ${trendWindow}`
  ];

  searchStatus.textContent = parts.join(" / ");
  persistDashboardState();
}

function restoreDashboardState() {
  const params = new URL(window.location.href).searchParams;
  const storedState = readStoredState();

  searchQuery.value = params.get("q") ?? storedState.q ?? "";
  setSelectedStage(normalizeStage(params.get("stage") ?? storedState.stage));
  selectedTag = params.get("tag") ?? storedState.tag ?? "";
  freshnessFilter.value = normalizeFreshnessValue(params.get("freshness") ?? storedState.freshness);
  searchSort.value = normalizeSortValue(params.get("sort") ?? storedState.sort);
  setTrendWindow(params.get("trend") ?? storedState.trend ?? "7d");
  selectedInspectorId = params.get("inspect") ?? storedState.inspect ?? "";
  const restoredTab = params.get("tab") ?? storedState.tab ?? (selectedInspectorId ? "inspect" : "search");
  setActiveDashboardTab(restoredTab);
}

function persistDashboardState() {
  const state = {
    q: searchQuery.value.trim(),
    stage: selectedStage !== "all" ? selectedStage : "",
    tag: selectedTag,
    freshness: freshnessFilter.value !== "all" ? freshnessFilter.value : "",
    sort: searchSort.value !== "newest" ? searchSort.value : "",
    trend: trendWindow !== "7d" ? trendWindow : "",
    inspect: selectedInspectorId,
    tab: activeDashboardTab !== "search" ? activeDashboardTab : ""
  };

  const url = new URL(window.location.href);
  setOrDeleteQueryValue(url.searchParams, "q", state.q);
  setOrDeleteQueryValue(url.searchParams, "stage", state.stage);
  setOrDeleteQueryValue(url.searchParams, "tag", state.tag);
  setOrDeleteQueryValue(url.searchParams, "freshness", state.freshness);
  setOrDeleteQueryValue(url.searchParams, "sort", state.sort);
  setOrDeleteQueryValue(url.searchParams, "trend", state.trend);
  setOrDeleteQueryValue(url.searchParams, "inspect", state.inspect);
  setOrDeleteQueryValue(url.searchParams, "tab", state.tab);
  history.replaceState({}, "", url);

  try {
    localStorage.setItem(dashboardStateStorageKey, JSON.stringify(state));
  } catch {
  }
}

function readStoredState() {
  try {
    return JSON.parse(localStorage.getItem(dashboardStateStorageKey) ?? "{}");
  } catch {
    return {};
  }
}

function setOrDeleteQueryValue(searchParams, key, value) {
  if (value) {
    searchParams.set(key, value);
    return;
  }

  searchParams.delete(key);
}

function hasSearchIntent() {
  return Boolean(searchQuery.value.trim()) || Boolean(selectedTag) || selectedStage !== "all" || freshnessFilter.value !== "all";
}

async function inspectGoldEntry(entryId) {
  selectedInspectorId = entryId;
  setActiveDashboardTab("inspect");
  persistDashboardState();
  inspectorMeta.textContent = "gold detail / history / related を読み込み中...";
  inspectorEmpty.hidden = true;
  inspectorPanel.hidden = false;
  inspectorPanel.innerHTML = '<div class="empty-state">詳細を読み込み中...</div>';

  try {
    const [detailResponse, historyResponse, relatedResponse] = await Promise.all([
      fetch(`/api/gold/catalog/${encodeURIComponent(entryId)}`, { headers: { Accept: "application/json" } }),
      fetch(`/api/gold/catalog/${encodeURIComponent(entryId)}/history?page=1&pageSize=5`, { headers: { Accept: "application/json" } }),
      fetch(`/api/gold/catalog/${encodeURIComponent(entryId)}/related?limit=5`, { headers: { Accept: "application/json" } })
    ]);

    if (!detailResponse.ok || !historyResponse.ok || !relatedResponse.ok) {
      throw new Error(`inspect request failed: ${detailResponse.status}/${historyResponse.status}/${relatedResponse.status}`);
    }

    const detail = await detailResponse.json();
    const history = await historyResponse.json();
    const related = await relatedResponse.json();
    renderInspector(detail, history, related);
    inspectorMeta.textContent = `${detail.displayName} / history ${history.totalCount} 件 / related ${related.items.length} 件`;
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    inspectorPanel.hidden = false;
    inspectorPanel.innerHTML = `<div class="empty-state">読み込みに失敗しました: ${escapeHtml(message)}</div>`;
    inspectorMeta.textContent = `inspect error: ${message}`;
  }
}

function clearInspector() {
  selectedInspectorId = "";
  inspectorPanel.hidden = true;
  inspectorPanel.replaceChildren();
  inspectorEmpty.hidden = false;
  inspectorMeta.textContent = "gold result の Inspect を押すと detail / history / related を読み込みます。";
  persistDashboardState();
}

function renderInspector(detail, history, related) {
  const tagsMarkup = detail.tags.length === 0
    ? '<span class="tag-chip">tag なし</span>'
    : detail.tags.map((tag) => `<span class="tag-chip">${escapeHtml(tag)}</span>`).join("");

  const metadata = [
    ["Auth", detail.authenticationType],
    ["Published", formatTimestamp(detail.publishedAtUtc)],
    ["Updated", formatTimestamp(detail.updatedAtUtc)],
    ["Clients", detail.supportedClients.join(", ") || "なし"],
    ["History count", String(detail.historyCount)],
    ["Updated by", detail.updatedBy]
  ].map(([label, value]) => `
      <div class="inspector-meta-item">
        <strong>${escapeHtml(label)}</strong>
        <span>${escapeHtml(value)}</span>
      </div>`).join("");

  const referencesMarkup = detail.references.length === 0
    ? '<div class="inspector-list-item"><strong>Reference</strong><span class="inspector-list-item__meta">なし</span></div>'
    : detail.references.map((reference) => `
      <div class="inspector-list-item">
        <strong>${escapeHtml(reference.label)}</strong>
        <a class="inspector-anchor" href="${escapeAttribute(reference.url)}" target="_blank" rel="noreferrer">${escapeHtml(reference.url)}</a>
      </div>`).join("");

  const historyMarkup = history.items.length === 0
    ? '<div class="inspector-list-item"><strong>History</strong><span class="inspector-list-item__meta">履歴はありません。</span></div>'
    : history.items.map((item) => `
      <div class="inspector-list-item">
        <strong>${escapeHtml(item.action)}</strong>
        <span>${escapeHtml(item.summary)}</span>
        <span class="inspector-list-item__meta">${escapeHtml(item.changedBy)} / ${formatTimestamp(item.changedAtUtc)} / usedLlm=${item.usedLlm ? "true" : "false"}</span>
      </div>`).join("");

  const relatedMarkup = related.items.length === 0
    ? '<div class="inspector-list-item"><strong>Related</strong><span class="inspector-list-item__meta">関連エントリはありません。</span></div>'
    : related.items.map((item) => `
      <div class="inspector-list-item">
        <strong><a class="inspector-anchor" href="/api/gold/catalog/${escapeAttribute(item.id)}" target="_blank" rel="noreferrer">${escapeHtml(item.displayName)}</a></strong>
        <span>${escapeHtml(item.overview)}</span>
        <span class="inspector-list-item__meta">score=${escapeHtml(String(item.score))} / tags=${escapeHtml((item.sharedTags || []).join(", ") || "なし")} / clients=${escapeHtml((item.sharedClients || []).join(", ") || "なし")}</span>
        <div class="inspector-inline-actions">
          <button class="button button--ghost button--compact inspector-related-action" type="button" data-entry-id="${escapeAttribute(item.id)}">Inspect</button>
        </div>
      </div>`).join("");

  inspectorPanel.hidden = false;
  inspectorPanel.innerHTML = `
    <article class="inspector-card reveal">
      <div class="inspector-card__header">
        <div>
          <p class="section-kicker">Detail</p>
          <h3>${escapeHtml(detail.displayName)}</h3>
        </div>
        <a class="button button--ghost button--compact" href="/api/gold/catalog/${escapeAttribute(detail.id)}" target="_blank" rel="noreferrer">Detail JSON</a>
      </div>
      <p class="inspector-copy">${escapeHtml(detail.overview)}</p>
      <div class="tag-cloud">${tagsMarkup}</div>
      <div class="inspector-inline-actions">
        <a class="button button--ghost button--compact" href="/api/gold/catalog/${escapeAttribute(detail.id)}/history?page=1&pageSize=20" target="_blank" rel="noreferrer">History JSON</a>
        <a class="button button--ghost button--compact" href="/api/gold/catalog/${escapeAttribute(detail.id)}/related?limit=10" target="_blank" rel="noreferrer">Related JSON</a>
      </div>
      <div class="inspector-meta-grid">${metadata}</div>
      <div class="inspector-list">
        <div class="inspector-list-item">
          <strong>Setup</strong>
          <pre class="inspector-pre">${escapeHtml(detail.setupGuide || "なし")}</pre>
        </div>
      </div>
      <div class="inspector-list">${referencesMarkup}</div>
    </article>
    <article class="inspector-card reveal">
      <div class="inspector-card__header">
        <div>
          <p class="section-kicker">History</p>
          <h3>変更履歴</h3>
        </div>
      </div>
      <div class="inspector-list">${historyMarkup}</div>
    </article>
    <article class="inspector-card reveal">
      <div class="inspector-card__header">
        <div>
          <p class="section-kicker">Related</p>
          <h3>関連エントリ</h3>
        </div>
      </div>
      <div class="inspector-list">${relatedMarkup}</div>
    </article>`;

  for (const button of inspectorPanel.querySelectorAll(".inspector-related-action")) {
    button.addEventListener("click", async () => {
      const entryId = button.dataset.entryId;
      if (entryId) {
        await inspectGoldEntry(entryId);
      }
    });
  }
}

function normalizeStage(value) {
  return value === "bronze" || value === "silver" || value === "gold" ? value : "all";
}

function normalizeFreshnessValue(value) {
  return value === "24h" || value === "7d" || value === "older" ? value : "all";
}

function normalizeSortValue(value) {
  return value === "oldest" || value === "title" || value === "stage" ? value : "newest";
}

function normalizeTrendWindow(value) {
  return value === "3d" ? "3d" : "7d";
}

function normalizeTab(value) {
  return value === "analytics" || value === "inspect" ? value : "search";
}

function setLoadingState(element, message) {
  element.className = `${element.id === "tag-cloud" ? "tag-cloud" : element.className.split(" ")[0]} empty-state`;
  element.textContent = message;
}

function renderError(element, error) {
  const message = error instanceof Error ? error.message : String(error);
  element.className = `${element.id === "tag-cloud" ? "tag-cloud" : element.className.split(" ")[0]} empty-state`;
  element.textContent = `読み込みに失敗しました: ${message}`;
}

function formatTimestamp(value) {
  if (!value) {
    return "なし";
  }

  const date = new Date(value);
  return new Intl.DateTimeFormat("ja-JP", {
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  }).format(date);
}

function freshnessHint(freshness) {
  return `24h ${freshness.last24Hours} / 7d ${freshness.last7Days} / older ${freshness.older}`;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function escapeAttribute(value) {
  return escapeHtml(value);
}
