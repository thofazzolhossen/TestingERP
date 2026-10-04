// =========================================================
// COMMON REUSABLE AG GRID (ABP.io)
// =========================================================

class CheckboxSetFilter {
    init(params) {
        this.params = params;
        this.values = [];
        this.selected = new Set();
        this.filterActive = false;

        params.api.forEachNode(node => {
            const value = params.getValue(node);
            if (value !== null && value !== undefined) {
                const strValue = String(value);
                if (!this.values.includes(strValue)) this.values.push(strValue);
            }
        });

        this.values.sort();
        this.values.forEach(v => this.selected.add(v));

        this.eGui = document.createElement("div");
        this.eGui.className = "card border shadow-lg p-0 rounded-3 overflow-hidden bg-white";
        this.eGui.style.width = "250px";
        this.eGui.style.fontSize = "13px";

        this.eGui.innerHTML = `
            <div class="p-2 bg-white border-bottom border-light-subtle">
                <div class="input-group input-group-sm">
                    <span class="input-group-text bg-white border-end-0 text-muted ps-2 pe-1">
                        <i class="bi bi-search" style="font-size: 0.75rem;"></i>
                    </span>
                    <input type="text" class="filter-search form-control border-start-0 ps-1 bg-white" placeholder="Search..." autocomplete="off">
                </div>
            </div>
            <div class="d-flex justify-content-between align-items-center px-3 py-2 bg-white border-bottom border-light-subtle text-muted" style="font-size: 12px;">
                <label class="d-flex align-items-center gap-2 cursor-pointer fw-semibold text-dark mb-0" style="cursor: pointer;">
                    <input type="checkbox" class="select-all form-check-input mt-0" style="cursor: pointer;">
                    <span>Select All</span>
                </label>
                <span class="selection-count text-secondary fw-normal"></span>
            </div>
            <div class="filter-values overflow-y-auto py-1" style="max-height: 180px;"></div>
            <div class="d-flex justify-content-between align-items-center p-2 bg-white border-top border-light-subtle">
                <button type="button" class="btn-clear btn btn-link text-decoration-none text-muted p-0 ms-1" style="font-size: 12px;">Clear filter</button>
                <button type="button" class="btn-apply btn btn-primary btn-sm px-2.5 py-0.5 rounded-2" style="font-size: 12px;">Apply</button>
            </div>
        `;

        this.searchBox = this.eGui.querySelector(".filter-search");
        this.selectAll = this.eGui.querySelector(".select-all");
        this.valueContainer = this.eGui.querySelector(".filter-values");
        this.countLabel = this.eGui.querySelector(".selection-count");
        this.clearBtn = this.eGui.querySelector(".btn-clear");
        this.applyBtn = this.eGui.querySelector(".btn-apply");

        ["keydown", "keyup", "keypress", "mousedown", "click"].forEach(eventType => {
            this.searchBox.addEventListener(eventType, e => e.stopPropagation());
        });

        this.searchBox.addEventListener("input", () => this.renderValues());

        this.selectAll.addEventListener("change", () => {
            const visibleValues = this.getVisibleValues();
            if (this.selectAll.checked) {
                visibleValues.forEach(v => this.selected.add(v));
            } else {
                visibleValues.forEach(v => this.selected.delete(v));
            }
            this.updateFilterState();
            this.renderValues();
            this.params.filterChangedCallback();
        });

        this.clearBtn.addEventListener("click", () => {
            this.selected.clear();
            this.values.forEach(v => this.selected.add(v));
            this.searchBox.value = "";
            this.updateFilterState();
            this.renderValues();
            this.params.filterChangedCallback();
        });

        this.applyBtn.addEventListener("click", () => {
            if (typeof this.params.hidePopup === 'function') {
                this.params.hidePopup();
            }
        });

        this.renderValues();
    }

    getGui() { return this.eGui; }

    getVisibleValues() {
        const search = this.searchBox.value.trim().toLowerCase();
        if (!search) return this.values;
        return this.values.filter(value => value.toLowerCase().includes(search));
    }

    renderValues() {
        const visibleValues = this.getVisibleValues();
        this.valueContainer.innerHTML = "";

        if (visibleValues.length === 0) {
            const emptyState = document.createElement("div");
            emptyState.className = "text-center text-muted py-3 px-2";
            emptyState.style.fontSize = "12px";
            emptyState.textContent = "No matches found";
            this.valueContainer.appendChild(emptyState);
            this.updateSelectAll();
            this.updateCountLabel();
            return;
        }

        visibleValues.forEach(value => {
            const label = document.createElement("label");
            label.className = "d-flex align-items-center gap-2 px-3 py-1.5 text-dark w-100 mb-0";
            label.style.cursor = "pointer";
            label.style.transition = "background-color 0.15s ease-in-out";
            label.onmouseover = () => label.style.backgroundColor = "#f8f9fa";
            label.onmouseout = () => label.style.backgroundColor = "transparent";

            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.className = "form-check-input mt-0";
            checkbox.style.cursor = "pointer";
            checkbox.checked = this.selected.has(value);

            const text = document.createElement("span");
            text.textContent = value;
            text.className = "text-truncate fw-normal";
            text.style.color = "#333";

            checkbox.addEventListener("change", () => {
                if (checkbox.checked) this.selected.add(value);
                else this.selected.delete(value);
                this.updateFilterState();
                this.updateSelectAll();
                this.updateCountLabel();
                this.params.filterChangedCallback();
            });

            label.appendChild(checkbox);
            label.appendChild(text);
            this.valueContainer.appendChild(label);
        });

        this.updateSelectAll();
        this.updateCountLabel();
    }

    updateSelectAll() {
        const visibleValues = this.getVisibleValues();
        if (visibleValues.length === 0) {
            this.selectAll.checked = false;
            this.selectAll.indeterminate = false;
            return;
        }
        const selectedCount = visibleValues.filter(v => this.selected.has(v)).length;
        if (selectedCount === visibleValues.length) {
            this.selectAll.checked = true;
            this.selectAll.indeterminate = false;
        } else if (selectedCount === 0) {
            this.selectAll.checked = false;
            this.selectAll.indeterminate = false;
        } else {
            this.selectAll.checked = false;
            this.selectAll.indeterminate = true;
        }
    }

    updateCountLabel() {
        const visibleValues = this.getVisibleValues();
        const selectedVisibleCount = visibleValues.filter(v => this.selected.has(v)).length;
        this.countLabel.textContent = `${selectedVisibleCount} of ${visibleValues.length}`;
    }

    updateFilterState() {
        this.filterActive = this.selected.size !== this.values.length;
    }

    isFilterActive() { return this.filterActive; }

    doesFilterPass(params) {
        if (!this.filterActive) return true;
        const value = String(this.params.getValue(params.node));
        return this.selected.has(value);
    }

    getModel() {
        return !this.filterActive ? null : { values: [...this.selected] };
    }

    setModel(model) {
        this.selected.clear();
        if (!model) {
            this.values.forEach(v => this.selected.add(v));
            this.filterActive = false;
        } else {
            model.values.forEach(v => this.selected.add(String(v)));
            this.filterActive = true;
        }
        if (this.searchBox) this.renderValues();
    }

    afterGuiAttached() {
        setTimeout(() => this.searchBox.focus(), 50);
    }
}

/**
 * Creates a reusable professional AG Grid
 * @param {Object} config
 * @param {string} config.gridId          - Required. DOM id of the grid container
 * @param {string} [config.title]         - Grid title (shown in header)
 * @param {Array}  [config.columnDefs]    - AG Grid columnDefs
 * @param {Array|Function} [config.rowData] - Static data OR function that returns Promise/Array
 * @param {Array}  [config.buttons]       - Extra buttons in the header
 * @param {string} [config.height]        - Grid height (default: 520px)
 * @param {Object} [config.gridOptions]   - Extra AG Grid options (merged)
 * @param {boolean}[config.showSearch]    - Show global search (default: true)
 * @param {boolean}[config.showExport]    - Show Export CSV button (default: true)
 * @param {string} [config.exportFileName]- CSV file name
 * @param {Function} [config.onGridReady] - Callback after grid is created
 */
window.createCommonGrid = function (config) {
    if (!config || !config.gridId) {
        console.error("createCommonGrid: gridId is required");
        return null;
    }

    const gridId = config.gridId;
    const title = config.title;
    const height = config.height || "520px";
    const showSearch = config.showSearch !== false;
    const showExport = config.showExport !== false;
    const exportFileName = config.exportFileName || `${gridId}.csv`;
    const buttons = config.buttons || [];

    // Find or create the outer card structure
    let gridDiv = document.getElementById(gridId);
    if (!gridDiv) {
        console.error(`createCommonGrid: Element #${gridId} not found`);
        return null;
    }

    // Build professional card UI if not already present
    if (!gridDiv.closest(".common-ag-card")) {
        const wrapper = document.createElement("div");
        wrapper.className = "card shadow-sm border rounded-3 overflow-hidden p-1 common-ag-card";
        wrapper.innerHTML = `
            <div class="card-body bg-white d-flex justify-content-between align-items-center mb-0" id="${gridId}_header">
                <div>
                    <h5 class="mb-0 fw-semibold text-dark" id="${gridId}_title">${title}</h5>
                    <span id="${gridId}_rowCount" class="text-muted small">Loading entries...</span>
                </div>
                <div class="d-flex align-items-center gap-2">
                    ${showSearch ? `
                    <input type="text"
                           id="${gridId}_search"
                           class="form-control form-control-sm"
                           placeholder="Search..."
                           style="width: 200px;">
                    ` : ""}
                    <div id="${gridId}_buttonGroup" class="d-flex align-items-center gap-2"></div>
                </div>
            </div>
            <div id="${gridId}_container"
                 style="height: ${height}; width: 100%;"
                 class="ag-theme-quartz"></div>
        `;

        // Move the original div into the new structure
        gridDiv.parentNode.insertBefore(wrapper, gridDiv);
        const container = wrapper.querySelector(`#${gridId}_container`);
        container.id = gridId; // keep original id on the actual grid element
        gridDiv.remove();
        gridDiv = container;
    } else {
        // Update title if already exists
        const titleEl = document.getElementById(`${gridId}_title`);
        if (titleEl) titleEl.textContent = title;
    }

    // Inject buttons
    const buttonGroup = document.getElementById(`${gridId}_buttonGroup`);
    if (buttonGroup) {
        buttonGroup.innerHTML = "";

        // Default Export button
        if (showExport) {
            const exportBtn = document.createElement("button");
            exportBtn.className = "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white text-secondary shadow-none";
            exportBtn.style.cursor = "pointer";
            exportBtn.innerHTML = `<i class="bi bi-download"></i> CSV`;
            exportBtn.onclick = () => {
                if (gridApi) {
                    gridApi.exportDataAsCsv({ fileName: exportFileName });
                }
            };
            buttonGroup.appendChild(exportBtn);
        }

        // Custom buttons from developer
        buttons.forEach(btn => {
            const el = document.createElement("button");
            el.className = btn.className || "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none";
            el.style.cursor = "pointer";
            el.id = btn.id || "";
            el.innerHTML = btn.html || btn.text || "Button";
            if (typeof btn.onClick === "function") {
                el.onclick = btn.onClick;
            }
            buttonGroup.appendChild(el);
        });
    }

    // Default column style
    const defaultColDef = {

        editable: false,
        filter: CheckboxSetFilter,
        sortable: true,
        resizable: true,
        cellStyle: {
            borderRight: "1px solid #dee2e6",
            borderBottom: "1px solid #dee2e6",
            display: "flex",
            alignItems: "center"
        },



        ...(config.defaultColDef || {})
    };

    // Serial number column helper
    const serialColumn = {
        headerName: "S.No",
        valueGetter: "node.rowIndex + 1",
        width: 80,
        flex: 0,
        filter: false,
        sortable: false,
        cellStyle: {
            borderLeft: "1px solid #dee2e6",
            borderRight: "1px solid #dee2e6",
            borderBottom: "1px solid #dee2e6",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontWeight: "600",
            color: "#6c757d"
        }
    };

    const columnDefs = config.columnDefs
        ? [serialColumn, ...config.columnDefs]
        : [serialColumn];

    const gridOptions = {

        autoSizeStrategy: {
            type: 'fitGridWidth'
        },
        alwaysShowHorizontalScroll: false,
        suppressHorizontalScroll: false,

        defaultColDef,
        columnDefs,
        pagination: true,
        paginationPageSize: 10,
        paginationPageSizeSelector: [10, 20, 50],
        onModelUpdated: () => {
            if (gridApi) {
                const totalRows = gridApi.getDisplayedRowCount();
                const countEl = document.getElementById(`${gridId}_rowCount`);
                if (countEl) {
                    countEl.textContent = `Showing ${totalRows} entries`;
                }
            }
        },
        ...(config.gridOptions || {})
    };

    // Create the grid
    const gridApi = agGrid.createGrid(gridDiv, gridOptions);

    // Quick search
    if (showSearch) {
        const searchInput = document.getElementById(`${gridId}_search`);
        if (searchInput) {
            searchInput.addEventListener("input", function () {
                gridApi.setGridOption("quickFilterText", this.value);
            });
        }
    }

    // Load data
    function setRowData(data) {
        gridApi.setGridOption("rowData", data || []);
    }

    if (typeof config.rowData === "function") {
        // Async loader
        Promise.resolve(config.rowData())
            .then(data => setRowData(data))
            .catch(err => {
                console.error("Failed to load grid data:", err);
                if (window.abp && abp.notify) {
                    abp.notify.error("Failed to load data.");
                }
                setRowData([]);
            });
    } else if (Array.isArray(config.rowData)) {
        setRowData(config.rowData);
    } else {
        setRowData([]);
    }

    // Public API returned to the developer
    const api = {
        gridApi,
        setRowData,
        refresh: () => {
            if (typeof config.rowData === "function") {
                Promise.resolve(config.rowData())
                    .then(data => setRowData(data));
            }
        },
        destroy: () => {
            if (gridApi) gridApi.destroy();
        }
    };

    if (typeof config.onGridReady === "function") {
        config.onGridReady(api);
    }

    // Store globally so you can access later if needed
    window[`gridApi_${gridId}`] = api;

    return api;
};