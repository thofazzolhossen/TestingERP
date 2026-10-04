$(document).ready(function () {

    // =========================================================
    // GRID INITIALIZATION
    // =========================================================
    window.leadStageGrid = createCommonGrid({
        gridId: "LeadStagesGrid",
        title: "Lead Stages",
        exportFileName: "lead-stages.csv",

        buttons: [
            {
                id: "BtnCreateNew",
                html: `<i class="bi bi-plus-circle"></i> Add`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    $('#LeadStageForm')[0].reset();
                    $('#StageId').val('');
                    $('#StageIsActive').prop('checked', true);
                    $('#ModalTitle').text('New Lead Stage');
                    $('#LeadStageModal').modal('show');
                }
            }
        ],

        columnDefs: [
            {
                field: "name",
                headerName: "Stage Name"
            },
            {
                field: "description",
                headerName: "Description",
                flex: 1.5,
                valueFormatter: params => params.value || '-'
            },
            {
                field: "isActive",
                headerName: "Status",
                width: 130,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return params.value
                        ? `<span class="badge bg-success-subtle text-success px-2 py-1"><i class="bi bi-check-circle me-1"></i>Active</span>`
                        : `<span class="badge bg-secondary-subtle text-secondary px-2 py-1"><i class="bi bi-x-circle me-1"></i>Inactive</span>`;
                }
            },
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
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger border-0 delete-button" 
                                    data-id="${params.data.id}" data-name="${params.data.name}" title="Delete">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],

        rowData: function () {
            return $.ajax({
                url: window.leadStageUrls.list,
                type: 'GET',
                data: { start: 0, length: 1000 }
            }).then(response => response.data || response.items || []);
        }
    });


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // ---------- OPEN EDIT MODAL ----------
    $(document).on('click', '#LeadStagesGrid .edit-button', function () {
        const encodedData = $(this).attr('data-row');
        if (!encodedData) return;

        const rowData = JSON.parse(decodeURIComponent(encodedData));

        $('#StageId').val(rowData.id || rowData.Id || '');
        $('#StageName').val(rowData.name || rowData.Name || '');
        $('#StageDescription').val(rowData.description || rowData.Description || '');
        $('#StageIsActive').prop('checked', Boolean(rowData.isActive ?? rowData.IsActive));

        $('#ModalTitle').text('Edit Lead Stage');
        $('#LeadStageModal').modal('show');
    });


    // ---------- SAVE (CREATE + EDIT) ----------
    $('#SaveButton').on('click', function () {
        const $btn = $(this);
        const id = $('#StageId').val();
        const isEdit = !!id;

        const name = $('#StageName').val();
        if (!name || name.trim() === '') {
            abp.notify.error('Please enter Lead Stage name.');
            return;
        }

        $btn.prop('disabled', true);

        const token = $('#LeadStageForm input[name="__RequestVerificationToken"]').val()
            || $('input[name="__RequestVerificationToken"]').val();

        const formData = {
            Name: name.trim(),
            Description: $('#StageDescription').val(),
            IsActive: $('#StageIsActive').is(':checked'),
            __RequestVerificationToken: token
        };

        let url = isEdit ? window.leadStageUrls.update : window.leadStageUrls.create;

        if (isEdit) {
            formData.id = id;
            // Some ABP handlers need id in query string
            if (url && !url.includes('id=')) {
                url += (url.includes('?') ? '&' : '?') + 'id=' + encodeURIComponent(id);
            }
        }

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    abp.notify.success(response.message || (isEdit ? 'Updated successfully' : 'Created successfully'));
                    $('#LeadStageModal').modal('hide');
                    $('#LeadStageForm')[0].reset();
                    window.leadStageGrid.refresh();
                } else {
                    abp.notify.error(response.message || 'Operation failed');
                }
            },
            error: function () {
                abp.notify.error(isEdit ? 'Update failed' : 'Create failed');
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });


    // ---------- DELETE ----------
    $(document).on('click', '#LeadStagesGrid .delete-button', function () {
        const id = $(this).data('id');
        const name = $(this).data('name');

        if (!confirm(`Are you sure you want to delete "${name}"?`)) return;

        const token = $('#LeadStageForm input[name="__RequestVerificationToken"]').val()
            || $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: window.leadStageUrls.delete,
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    abp.notify.success('Deleted successfully');
                    window.leadStageGrid.refresh();
                } else {
                    abp.notify.error(response.message || 'Failed to delete');
                }
            },
            error: function () {
                abp.notify.error('Failed to delete Lead Stage');
            }
        });
    });

});