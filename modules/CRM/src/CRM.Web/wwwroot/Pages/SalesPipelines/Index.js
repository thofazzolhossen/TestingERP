$(document).ready(function () {

    // =========================================================
    // GRID INITIALIZATION
    // =========================================================
    window.salesPipelineGrid = createCommonGrid({
        gridId: "SalesPipelinesGrid",
        title: "Sales Pipelines",
        exportFileName: "sales-pipelines.csv",
        height: "550px",

        buttons: [
            {
                id: "BtnCreateNew",
                html: `<i class="fa fa-plus"></i> New Sales Pipeline`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    $('#SalesPipelineForm')[0].reset();
                    $('#PipelineId').val('');
                    $('#PipelineIsActive').prop('checked', true);
                    $('#ModalTitle').text('New Sales Pipeline');
                    $('#SalesPipelineModal').modal('show');
                }
            }
        ],

        columnDefs: [
            {
                field: "name",
                headerName: "Pipeline Name",
                flex: 1.5
            },
            {
                field: "description",
                headerName: "Description",
                flex: 2,
                valueFormatter: params => params.value || '-'
            },
            {
                field: "isActive",
                headerName: "Status",
                width: 130,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return params.value
                        ? `<span class="badge bg-success-subtle text-success px-2 py-1"><i class="fa-solid fa-circle-check me-1"></i>Active</span>`
                        : `<span class="badge bg-secondary-subtle text-secondary px-2 py-1"><i class="fa-solid fa-circle-xmark me-1"></i>Inactive</span>`;
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
                                <i class="fa-solid fa-pen-to-square"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger border-0 delete-button" 
                                    data-id="${params.data.id}" data-name="${params.data.name}" title="Delete">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],

        // Client-side loading
        rowData: function () {
            return $.ajax({
                url: window.salesPipelineUrls.list,
                type: 'GET',
                data: { start: 0, length: 1000 }
            }).then(response => response.data || response.items || []);
        }
    });


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // ---------- OPEN EDIT MODAL ----------
    $(document).on('click', '#SalesPipelinesGrid .edit-button', function () {
        const rawData = $(this).attr('data-row');
        if (!rawData) return;

        const data = JSON.parse(decodeURIComponent(rawData));

        $('#PipelineId').val(data.id || data.Id || '');
        $('#PipelineName').val(data.name || data.Name || '');
        $('#PipelineDisplayOrder').val(data.displayOrder || data.DisplayOrder || 0);
        $('#PipelineDescription').val(data.description || data.Description || '');
        $('#PipelineIsActive').prop('checked', Boolean(data.isActive ?? data.IsActive));

        $('#ModalTitle').text('Edit Sales Pipeline');
        $('#SalesPipelineModal').modal('show');
    });


    // ---------- SAVE (CREATE + EDIT) ----------
    $('#SaveButton').on('click', function () {
        const $btn = $(this);
        const id = $('#PipelineId').val();
        const isEdit = !!id;

        const name = $('#PipelineName').val();
        if (!name || name.trim() === '') {
            abp.notify.error('Please enter a Sales Pipeline name.');
            return;
        }

        $btn.prop('disabled', true);

        const token = $('#SalesPipelineForm input[name="__RequestVerificationToken"]').val()
            || $('input[name="__RequestVerificationToken"]').val();

        const formData = {
            Name: name.trim(),
            Description: $('#PipelineDescription').val(),
            IsActive: $('#PipelineIsActive').is(':checked'),
            __RequestVerificationToken: token
        };

        // Include DisplayOrder if the field exists
        if ($('#PipelineDisplayOrder').length) {
            formData.DisplayOrder = $('#PipelineDisplayOrder').val() || 0;
        }

        let url = isEdit
            ? (window.salesPipelineUrls.update + `&id=${id}`)
            : window.salesPipelineUrls.create;

        $.ajax({
            url: url,
            type: 'POST',
            data: formData,
            success: function (response) {
                if (response.success) {
                    abp.notify.success(isEdit ? 'Sales Pipeline updated successfully.' : 'Sales Pipeline created successfully.');
                    $('#SalesPipelineModal').modal('hide');
                    $('#SalesPipelineForm')[0].reset();
                    window.salesPipelineGrid.refresh();
                } else {
                    abp.notify.error(response.message || 'Operation failed.');
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
    $(document).on('click', '#SalesPipelinesGrid .delete-button', function () {
        const id = $(this).data('id');
        const name = $(this).data('name');

        abp.message.confirm(`Are you sure you want to delete "${name}"?`, 'Confirm Deletion')
            .then(function (confirmed) {
                if (!confirmed) return;

                const token = $('input[name="__RequestVerificationToken"]').first().val();

                $.ajax({
                    url: window.salesPipelineUrls.delete,
                    type: 'POST',
                    data: {
                        id: id,
                        __RequestVerificationToken: token
                    },
                    success: function (response) {
                        if (response.success) {
                            abp.notify.success('Deleted successfully.');
                            window.salesPipelineGrid.refresh();
                        } else {
                            abp.notify.error(response.message || 'Failed to delete');
                        }
                    },
                    error: function () {
                        abp.notify.error('Failed to delete Sales Pipeline.');
                    }
                });
            });
    });

});