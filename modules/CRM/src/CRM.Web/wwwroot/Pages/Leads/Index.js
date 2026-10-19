$(document).ready(function () {

    // =========================================================
    // SELECT2 INITIALIZATION
    // =========================================================
    $('.select2-dropdown').select2({
        theme: 'bootstrap-5',
        dropdownParent: $('#LeadModal')
    });


    // =========================================================
    // GRID INITIALIZATION
    // =========================================================
    window.leadGrid = createCommonGrid({
        gridId: "LeadsGrid",
        title: "Leads",
        exportFileName: "leads.csv",
        height: "600px",

        // Add gridOptions here for stable row ID patching:
        gridOptions: {
            getRowId: p => p.data.id
        },

        buttons: [
            {
                id: "BtnUploadCsv",
                html: `<i class="fa-solid fa-file-csv me-1"></i> Upload by CSV`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-success text-success shadow-none me-2",
                onClick: function () {
                    $('#CsvUploadForm')[0].reset();
                    $('#UploadCsvModal').modal('show');
                }
            },
            {
                id: "BtnCreateNew",
                html: `<i class="fa fa-plus"></i> New Lead`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    openCreateModal();
                }
            }
        ],

        columnDefs: [
            { field: "name", headerName: "Lead Name", flex: 1.5 },
            { field: "companyName", headerName: "Company", flex: 1.2 },
            { field: "assignedUserName", headerName: "Assigned", flex: 1.2 },
            { field: "email", headerName: "Email", flex: 1.2 },
            { field: "phone", headerName: "Phone", width: 130 },
            { field: "customerTypeName", headerName: "Customer Type", flex: 1 },
            { field: "leadSourceName", headerName: "Source", flex: 1 },
            { field: "leadStageName", headerName: "Stage", flex: 1 },
            { field: "serviceName", headerName: "Service", flex: 1 },
            {
                headerName: "Actions",
                width: 120,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';

                    const encodedRow = encodeURIComponent(JSON.stringify(params.data));

                    return `
                        <div class="d-flex align-items-center h-100 gap-2">
                            <button type="button" class="btn btn-sm btn-outline-primary border-0 edit-button" 
                                    data-row="${encodedRow}" title="Edit">
                                <i class="fa fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger border-0 delete-button" 
                                    data-id="${params.data.id}" title="Delete">
                                <i class="fa fa-trash"></i>
                            </button>
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


    // =========================================================
    // HELPER FUNCTIONS
    // =========================================================
    function openCreateModal() {
        $('#LeadForm')[0].reset();
        $('#LeadId').val('');
        $('.select2-dropdown').val(null).trigger('change'); $('#ModalTitle').text('New Lead');
        $('#LeadModal').modal('show');
    }


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // ---------- OPEN EDIT MODAL ----------
    $(document).on('click', '#LeadsGrid .edit-button', function () {
        const rawData = $(this).attr('data-row');
        if (!rawData) return;

        const data = JSON.parse(decodeURIComponent(rawData));

        $('#LeadId').val(data.id || '');
        $('#LeadName').val(data.name || '');
        $('#LeadCompanyName').val(data.companyName || '');
        $('#LeadEmail').val(data.email || '');
        $('#LeadPhone').val(data.phone || '');
        $('#LeadEstimatedBudget').val(data.estimatedBudget || 0);
        $('#LeadRemarks').val(data.remarks || '');

        // Dates
        if (data.expectedCloseDate) {
            $('#LeadExpectedCloseDate').val(data.expectedCloseDate.substring(0, 10));
        } else {
            $('#LeadExpectedCloseDate').val('');
        }

        if (data.nextFollowUpDate) {
            $('#LeadNextFollowUpDate').val(data.nextFollowUpDate.substring(0, 10));
        } else {
            $('#LeadNextFollowUpDate').val('');
        }

        // Select2 dropdowns
        $('#AssignedUserId').val(data.assignedUserId || '').trigger('change');
        $('#CustomerTypeId').val(data.customerTypeId || '').trigger('change');
        $('#LeadSourceId').val(data.leadSourceId || '').trigger('change');
        $('#LeadStageId').val(data.leadStageId || '').trigger('change');
        $('#ServiceId').val(data.serviceId || '').trigger('change');

        $('#ModalTitle').text('Edit Lead');
        $('#LeadModal').modal('show');
    });


    // ---------- SAVE (CREATE + EDIT) ----------
    $('#SaveButton').on('click', function () {
        const $btn = $(this);
        const id = $('#LeadId').val();
        const isEdit = !!id;

        const name = $('#LeadName').val();
        if (!name || name.trim() === '') {
            abp.notify.error('Please enter Lead Name.');
            return;
        }

        $btn.prop('disabled', true);

        const url = isEdit
            ? `?handler=Update&id=${id}`
            : '?handler=Create';

        $.ajax({
            url: url,
            type: 'POST',
            data: $('#LeadForm').serialize(),
            success: function (res) {
                if (res.success) {
                    abp.notify.success(res.message || (isEdit ? 'Lead updated successfully' : 'Lead created successfully'));
                    $('#LeadModal').modal('hide');
                    window.leadGrid.refresh();
                } else {
                    abp.notify.error(res.message || 'Operation failed.');
                }
            },
            error: function () {
                abp.notify.error('Request failed.');
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });


    // ---------- DELETE ----------
    $(document).on('click', '#LeadsGrid .delete-button', function () {
        const id = $(this).data('id');

        abp.message.confirm('Are you sure you want to delete this lead?', 'Confirm Deletion')
            .then(function (confirmed) {
                if (!confirmed) return;

                $.ajax({
                    url: `?handler=Delete&id=${id}`,
                    type: 'POST',
                    headers: {
                        "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function (res) {
                        if (res.success) {
                            abp.notify.success(res.message || 'Deleted successfully');
                            window.leadGrid.refresh();
                        } else {
                            abp.notify.error(res.message || 'Failed to delete');
                        }
                    },
                    error: function () {
                        abp.notify.error('Failed to delete Lead');
                    }
                });
            });
    });


    // ---------- CSV UPLOAD FORM SUBMISSION ----------
    $('#CsvUploadForm').on('submit', function (e) {
        e.preventDefault();

        const fileInput = $('#CsvFileInput')[0];
        if (!fileInput.files.length) {
            abp.notify.error('Please choose a CSV file to upload.');
            return;
        }

        const formData = new FormData(this);
        const $btn = $('#SaveCsvButton');
        $btn.prop('disabled', true);

        $.ajax({
            url: '?handler=UploadCsv',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    abp.notify.success(response.message);
                    $('#UploadCsvModal').modal('hide');
                    $('#CsvUploadForm')[0].reset();
                    if (window.leadGrid && typeof window.leadGrid.refresh === 'function') {
                        window.leadGrid.refresh();
                    }
                } else {
                    abp.notify.error(response.message || 'CSV Import failed.');
                }
            },
            error: function (xhr) {
                console.error('CSV upload failed:', xhr.status, xhr.responseText);
                const msg = (xhr.responseJSON && xhr.responseJSON.error && xhr.responseJSON.error.message)
                    || `Upload failed (HTTP ${xhr.status} ${xhr.statusText})`;
                abp.notify.error(msg);
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });



    // ===== REAL-TIME (Google Sheet -> grid) =====
    const hub = new signalR.HubConnectionBuilder()
        .withUrl('/signalr-hubs/leads').withAutomaticReconnect().build();

    hub.on('LeadsUpserted', rows => {
        const api = window.leadGrid.gridApi, add = [], update = [];
        rows.forEach(r => (api.getRowNode(r.id) ? update : add).push(r));
        api.applyTransaction({ add, update, addIndex: 0 });   // O(changed rows)
        abp.notify.info(`${rows.length} lead(s) synced from Google Sheet`);
    });
    hub.onreconnected(() => window.leadGrid.refresh());       // catch up after a dropped connection
    hub.start().catch(console.error);
});