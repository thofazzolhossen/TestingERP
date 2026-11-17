$(document).ready(function () {

    window.dealGrid = createCommonGrid({
        gridId: "DealsGrid",
        title: "Deals",
        exportFileName: "deals.csv",
        height: "600px",

        gridOptions: {
            getRowId: p => p.data.id
        },

        buttons: [
            {
                id: "BtnGoToLeads",
                html: `<i class="fa fa-user-tie me-1"></i> Go to Leads`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    window.location.href = '/Leads';
                }
            }
        ],

        columnDefs: [
            { field: "name", headerName: "Deal / Lead Name", flex: 1.5 },
            { field: "companyName", headerName: "Company", flex: 1.2 },
            { field: "assignedUserName", headerName: "Assigned", flex: 1.2 },
            { field: "email", headerName: "Email", flex: 1.2 },
            { field: "phone", headerName: "Phone", width: 130 },
            { field: "customerTypeName", headerName: "Customer Type", flex: 1 },
            { field: "leadSourceName", headerName: "Source", flex: 1 },
            { field: "leadStageName", headerName: "Stage", flex: 1 },
            { field: "serviceName", headerName: "Service", flex: 1 },
            {
                field: "estimatedBudget",
                headerName: "Deal Value",
                width: 130,
                valueFormatter: p => p.value != null ? Number(p.value).toLocaleString(undefined, { minimumFractionDigits: 2 }) : "0.00"
            },
            {
                field: "convertedToDealTime",
                headerName: "Converted On",
                width: 135,
                valueFormatter: p => p.value ? p.value.substring(0, 10) : "-"
            },
            {
                headerName: "Actions",
                width: 150,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return `
                        <div class="d-flex align-items-center h-100 gap-1">
                            <button type="button"
                                    class="btn btn-sm btn-outline-warning border-0 back-to-lead-button"
                                    data-id="${params.data.id}"
                                    data-name="${params.data.name || ''}"
                                    title="Back to Lead">
                                <i class="fa-solid fa-rotate-left"></i>
                            </button>
                            <a href="/Leads/Details?id=${params.data.id}"
                               class="btn btn-sm btn-outline-info border-0 view-button"
                               title="View Deal Details">
                                <i class="fa fa-eye"></i>
                            </a>
                        </div>
                    `;
                }
            }
        ],

        rowData: function () {
            return $.ajax({
                url: '?handler=List',
                type: 'GET',
                data: { start: 0, length: 1000 }
            }).then(response => response.data || response.items || []);
        }
    });

    // ---------- BACK TO LEAD ----------
    $(document).on('click', '#DealsGrid .back-to-lead-button', function () {
        const $btn = $(this);
        const id = $btn.data('id');
        const name = $btn.data('name') || 'this deal';

        abp.message.confirm(
            `Are you sure you want to move "${name}" back to Lead?`,
            'Back to Lead'
        ).then(function (confirmed) {
            if (!confirmed) return;

            $btn.prop('disabled', true);

            $.ajax({
                url: `?handler=BackToLead&id=${id}`,
                type: 'POST',
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (res) {
                    if (res.success) {
                        abp.notify.success(res.message || 'Moved back to Lead!');

                        // O(1) row removal from Deals Grid
                        const api = window.dealGrid?.gridApi;
                        const rowNode = api ? api.getRowNode(id) : null;
                        if (rowNode) {
                            api.applyTransaction({ remove: [rowNode.data] });
                        } else if (window.dealGrid && typeof window.dealGrid.refresh === 'function') {
                            window.dealGrid.refresh();
                        }
                    } else {
                        abp.notify.error(res.message || 'Operation failed.');
                        $btn.prop('disabled', false);
                    }
                },
                error: function () {
                    abp.notify.error('Failed to move Deal back to Lead.');
                    $btn.prop('disabled', false);
                }
            });
        });
    });
});