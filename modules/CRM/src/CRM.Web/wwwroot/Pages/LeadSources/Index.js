$(document).ready(function () {

    window.leadSourceGrid = createCommonGrid({
        gridId: "LeadSourcesGrid",
        title: "Lead Sources",
        exportFileName: "lead-sources.csv",
        height: "550px",

        buttons: [
            {
                id: "BtnCreateNew",
                html: `<i class="fa fa-plus"></i> New Lead Source`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    $('#CreateForm')[0].reset();
                    $('#EntityId').val('');
                    $('#EntityIsActive').prop('checked', true);
                    $('#ModalTitle').text('New Lead Source');
                    $('#LeadSourchModal').modal('show');
                }
            }
        ],

        columnDefs: [
            {
                field: "name",
                headerName: "Name",
                flex: 1
            },
            {
                field: "description",
                headerName: "Description",
                flex: 1.5,
                valueFormatter: params => params.value || '-'
            },
            {
                field: "isActive",
                headerName: "Is Active",
                width: 120,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return params.value
                        ? `<span class="badge bg-success-subtle text-success px-2 py-1">Active</span>`
                        : `<span class="badge bg-secondary-subtle text-secondary px-2 py-1">Inactive</span>`;
                }
            },
            {
                headerName: "Actions",
                width: 140,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';

                    // Very important: encode the whole object
                    const encodedRow = encodeURIComponent(JSON.stringify(params.data));

                    return `
            <div class="d-flex align-items-center h-100 gap-2">
                            <button type="button" class="btn btn-sm btn-outline-primary border-0 edit-button" data-row="${encodedRow}" title="Edit">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger border-0 delete-button" data-id="${params.data.id}" data-name="${params.data.name}" title="Delete">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
        `;
                }
            }
        ],

        rowData: function () {
            return $.ajax({
                url: window.leadSourceUrls.list,
                type: 'GET',
                data: { start: 0, length: 1000 }
            }).then(response => response.data || response.items || []);
        }
    });


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // ---------- OPEN EDIT MODAL ----------
    $(document).on('click', '#LeadSourcesGrid .edit-button', function () {
        const encodedData = $(this).attr('data-row');
        if (!encodedData) return;

        // 1. Decode and parse row data
        const rowData = JSON.parse(decodeURIComponent(encodedData));

        // 2. Populate form fields using row properties
        $('#EntityId').val(rowData.id || rowData.Id);
        $('#EntityName').val(rowData.name || rowData.Name);
        $('#EntityDescription').val(rowData.description || rowData.Description || '');
        $('#EntityIsActive').prop('checked', Boolean(rowData.isActive ?? rowData.IsActive));

        // 3. Set Modal Title and show modal
        $('#ModalTitle').text('Edit Lead Source');
        $('#LeadSourchModal').modal('show');
    });

    // ---------- SAVE (Create + Edit) ----------
    $('#SaveButton').on('click', function () {
        const $btn = $(this);
        const id = $('#EntityId').val();
        const isEdit = !!id;

        const name = $('#EntityName').val();
        if (!name || name.trim() === '') {
            abp.notify.error('Please enter Lead Source name.');
            return;
        }

        $btn.prop('disabled', true);

        // Build the request data payload
        const formData = {
            Name: name.trim(),
            Description: $('#EntityDescription').val(),
            IsActive: $('#EntityIsActive').is(':checked'),
            __RequestVerificationToken: $('#CreateForm input[name="__RequestVerificationToken"]').val()
        };

        // Determine target URL
        let url = isEdit ? window.leadSourceUrls.update : window.leadSourceUrls.create;

        // For Razor Pages OnPostUpdateAsync(Guid id, CreateUpdateLeadSourceDto input)
        if (isEdit) {
            formData.id = id;
            // Append query param if your route URL expects ?id=... or handler routing
            if (!url.includes('id=')) {
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
                    $('#LeadSourchModal').modal('hide');
                    $('#CreateForm')[0].reset();
                    window.leadSourceGrid.refresh();
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
    $(document).on('click', '#LeadSourcesGrid .delete-button', function () {
        const id = $(this).data('id');
        const name = $(this).data('name');

        if (!confirm(`Are you sure you want to delete "${name}"?`)) return;

        $.ajax({
            url: window.leadSourceUrls.delete,
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    abp.notify.success('Deleted successfully');
                    window.leadSourceGrid.refresh();
                } else {
                    abp.notify.error(response.message || 'Failed to delete');
                }
            },
            error: function () {
                abp.notify.error('Failed to delete Lead Source');
            }
        });
    });

});