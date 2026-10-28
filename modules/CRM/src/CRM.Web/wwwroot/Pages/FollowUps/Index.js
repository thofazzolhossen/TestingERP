$(document).ready(function () {
    let allRangeRows = [];
    let currentKpiStatus = 'ALL';

    $('.select2-followup').select2({
        theme: 'bootstrap-5',
        dropdownParent: $('#FollowUpModal'),
        width: '100%'
    });


    // Helper: Format Date to YYYY-MM-DD in local time
    function toLocalIsoDate(dateObj) {
        const y = dateObj.getFullYear();
        const m = String(dateObj.getMonth() + 1).padStart(2, '0');
        const d = String(dateObj.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    // Helper: Safely parse YYYY-MM-DD, M/D/YYYY, or D/M/YYYY into a timestamp for comparison
    function parseDateValue(val) {
        if (!val || typeof val !== 'string') return null;
        const trimmed = val.trim();
        if (!trimmed) return null;

        if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
            const [y, m, d] = trimmed.split('-').map(Number);
            return new Date(y, m - 1, d).getTime();
        }

        const parsed = new Date(trimmed).getTime();
        return isNaN(parsed) ? null : parsed;
    }

    const todayStr = toLocalIsoDate(new Date());

    // Classify a follow-up date into OVERDUE, TODAY, or UPCOMING in O(1)
    function computeFollowUpStatus(nextFollowUpDateStr) {
        if (!nextFollowUpDateStr) return 'NONE';
        const datePart = nextFollowUpDateStr.substring(0, 10);
        if (datePart < todayStr) return 'OVERDUE';
        if (datePart === todayStr) return 'TODAY';
        return 'UPCOMING';
    }

    // Update the 4 KPI Card Counters in O(M) single pass
    function updateKpiCounters(rows) {
        let total = rows.length;
        let overdue = 0;
        let dueToday = 0;
        let upcoming = 0;

        for (let i = 0; i < rows.length; i++) {
            const st = rows[i]._followUpStatus;
            if (st === 'OVERDUE') overdue++;
            else if (st === 'TODAY') dueToday++;
            else if (st === 'UPCOMING') upcoming++;
        }

        $('#KpiTotalCount').text(total);
        $('#KpiOverdueCount').text(overdue);
        $('#KpiTodayCount').text(dueToday);
        $('#KpiUpcomingCount').text(upcoming);
    }

    // Helper: Safely get the underlying AG Grid API from createCommonGrid
    function getGridApi() {
        if (!window.followUpGrid) return null;
        return window.followUpGrid.gridApi || window.followUpGrid.api || window.followUpGrid;
    }

    // Helper: Update AG Grid rows and entry counter
    function setGridRows(rows) {
        const api = getGridApi();
        if (api) {
            if (typeof api.setGridOption === 'function') {
                api.setGridOption('rowData', rows);
            } else if (typeof api.setRowData === 'function') {
                api.setRowData(rows);
            }
        }

        const countEl = document.querySelector('#FollowUpsGrid #rowCount, #rowCount');
        if (countEl) {
            countEl.textContent = `Showing ${rows.length} entries`;
        }
    }

    // Apply KPI Card Status Filter in O(M) memory without extra DB round-trip
    function applyKpiFilter() {
        if (currentKpiStatus === 'ALL') {
            setGridRows(allRangeRows);
        } else {
            const filtered = allRangeRows.filter(r => r._followUpStatus === currentKpiStatus);
            setGridRows(filtered);
        }
    }

    // Fetch Date-Range Filtered Data from Server
    function fetchFollowUpData() {
        const fromDate = $('#FilterFromDate').val() || '';
        const toDate = $('#FilterToDate').val() || '';

        return $.ajax({
            url: '?handler=List',
            type: 'GET',
            data: {
                fromDate: fromDate,
                toDate: toDate
            }
        }).then(function (response) {
            const items = response.data || response.items || [];
            for (let i = 0; i < items.length; i++) {
                items[i]._followUpStatus = computeFollowUpStatus(items[i].nextFollowUpDate);
            }
            allRangeRows = items;
            updateKpiCounters(allRangeRows);

            if (currentKpiStatus === 'ALL') {
                return allRangeRows;
            }
            return allRangeRows.filter(r => r._followUpStatus === currentKpiStatus);
        });
    }

    // Reload Grid from Server using createCommonGrid's refresh() or direct setGridRows()
    function reloadGridFromServer() {
        if (window.followUpGrid && typeof window.followUpGrid.refresh === 'function') {
            window.followUpGrid.refresh();
            return;
        }

        fetchFollowUpData().then(function (rowsToRender) {
            setGridRows(rowsToRender);
        });
    }

    // =========================================================
    // INITIALIZE AG GRID (SAME STYLE AS LEADS GRID)
    // =========================================================
    window.followUpGrid = createCommonGrid({
        gridId: "FollowUpsGrid",
        title: "Follow-Ups Schedule",
        exportFileName: "lead-follow-ups.csv",
        height: "560px",
        gridOptions: {
            getRowId: p => p.data.id
        },
        buttons: [
            {
                id: "BtnGoToLeads",
                html: `<i class="fa fa-user-tie me-1"></i> All Leads`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    window.location.href = '/Leads';
                }
            }
        ],
        columnDefs: [
            {
                field: "nextFollowUpDate",
                filter: false,
                headerName: "Follow-Up Date",
                width: 145,
                valueFormatter: p => p.value ? p.value.substring(0, 10) : '-'
            },
            {
                field: "_followUpStatus",
                headerName: "Status",
                width: 135,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    const st = params.value;
                    if (st === 'OVERDUE') {
                        return `<span class="badge bg-danger-subtle text-danger border border-danger-subtle px-2 py-1"><i class="fa fa-circle-exclamation me-1"></i>Overdue</span>`;
                    }
                    if (st === 'TODAY') {
                        return `<span class="badge bg-warning-subtle text-warning-emphasis border border-warning-subtle px-2 py-1"><i class="fa fa-bell me-1"></i>Due Today</span>`;
                    }
                    return `<span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1"><i class="fa fa-clock me-1"></i>Upcoming</span>`;
                }
            },
            {
                field: "name",
                headerName: "Lead Name",
                flex: 1.4,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return `<a href="/Leads/Details?id=${params.data.id}" class="fw-semibold text-primary text-decoration-none">${params.value || '-'}</a>`;
                }
            },
            { field: "companyName", headerName: "Company", flex: 1.2 },
            { field: "phone", headerName: "Phone", width: 130 },
            { field: "assignedUserName", headerName: "Assigned", flex: 1.1 },
            {
                field: "leadStageName",
                headerName: "Stage",
                flex: 1,
                cellRenderer: function (params) {
                    if (!params.value) return '-';
                    return `<span class="badge bg-primary-subtle text-primary px-2 py-1">${params.value}</span>`;
                }
            },
            { field: "serviceName", headerName: "Service", flex: 1 },
            {
                field: "remarks",
                headerName: "Remarks",
                flex: 1.4,
                valueFormatter: p => p.value || '-'
            },
            {
                headerName: "Actions",
                width: 130,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    const encodedRow = encodeURIComponent(JSON.stringify(params.data));
                    return `
                        <div class="d-flex align-items-center h-100 gap-1">
                            <a href="/Leads/Details?id=${params.data.id}"
                               class="btn btn-sm btn-outline-info border-0"
                               title="View Lead Details">
                                <i class="fa fa-eye"></i>
                            </a>
                            <button type="button"
                                    class="btn btn-sm btn-outline-primary border-0 btn-reschedule"
                                    data-row="${encodedRow}"
                                    title="Update / Reschedule Follow-Up">
                                <i class="fa fa-calendar-plus"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        rowData: function () {
            return fetchFollowUpData();
        }
    });

    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // 1. Load button (From & To Date Range)
    $('#BtnFilterLoad').on('click', function () {
        const fromTime = parseDateValue($('#FilterFromDate').val());
        const toTime = parseDateValue($('#FilterToDate').val());

        if (fromTime !== null && toTime !== null && fromTime > toTime) {
            abp.notify.warn('"From Date" cannot be later than "To Date".');
            return;
        }
        reloadGridFromServer();
    });

    // 2. Reset Date Filter
    $('#BtnFilterReset').on('click', function () {
        $('#FilterFromDate').val('');
        $('#FilterToDate').val('');
        currentKpiStatus = 'ALL';
        $('.kpi-card').removeClass('active-kpi');
        $('.kpi-card[data-status="ALL"]').addClass('active-kpi');
        reloadGridFromServer();
    });

    // 3. Quick Date Range Preset Buttons (Today, Next 7 Days, This Month)
    $('.preset-range').on('click', function () {
        const range = $(this).data('range');
        const now = new Date();
        let start = new Date(now);
        let end = new Date(now);

        if (range === 'today') {
            // Today only
        } else if (range === 'week') {
            end.setDate(now.getDate() + 7);
        } else if (range === 'month') {
            start = new Date(now.getFullYear(), now.getMonth(), 1);
            end = new Date(now.getFullYear(), now.getMonth() + 1, 0);
        }

        $('#FilterFromDate').val(toLocalIsoDate(start));
        $('#FilterToDate').val(toLocalIsoDate(end));
        reloadGridFromServer();
    });

    // 4. KPI Card Click Filter (O(1) toggle)
    $('.kpi-card').on('click', function () {
        $('.kpi-card').removeClass('active-kpi');
        $(this).addClass('active-kpi');
        currentKpiStatus = $(this).data('status') || 'ALL';
        applyKpiFilter();
    });

    // 5. Open Update / Reschedule Follow-Up Modal
    $(document).on('click', '#FollowUpsGrid .btn-reschedule', function () {
        const raw = $(this).attr('data-row');
        if (!raw) return;

        const data = JSON.parse(decodeURIComponent(raw));
        $('#FollowUpLeadId').val(data.id);
        $('#ModalLeadName').text(`${data.name || '-'} (${data.companyName || 'No Company'})`);
        $('#ModalLeadSubInfo').text(`Phone: ${data.phone || '-'} | Email: ${data.email || '-'}`);

        if (data.nextFollowUpDate) {
            $('#FollowUpNextDate').val(data.nextFollowUpDate.substring(0, 10));
        } else {
            $('#FollowUpNextDate').val('');
        }

        $('#FollowUpStageId').val(data.leadStageId || '').trigger('change');
        $('#FollowUpAssignedUserId').val(data.assignedUserId || '').trigger('change');
        $('#FollowUpRemarks').val(data.remarks || '');

        $('#FollowUpModal').modal('show');
    });

    // 6. Save Rescheduled Follow-Up
    $('#BtnSaveFollowUp').on('click', function () {
        const $btn = $(this);
        $btn.prop('disabled', true);

        $.ajax({
            url: '?handler=Reschedule',
            type: 'POST',
            data: $('#FollowUpForm').serialize(),
            success: function (res) {
                if (res && res.success) {
                    abp.notify.success(res.message || 'Follow-up updated!');
                    $('#FollowUpModal').modal('hide');
                    reloadGridFromServer();
                } else {
                    abp.notify.error(res?.message || 'Failed to update follow-up.');
                }
            },
            error: function () {
                abp.notify.error('Error occurred while saving follow-up.');
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });
});