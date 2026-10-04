$(document).ready(function () {

    // =========================================================
    // GRID INITIALIZATION
    // =========================================================
    window.serviceGrid = createCommonGrid({
        gridId: "ServicesGrid",
        title: "Services",
        exportFileName: "services.csv",
        height: "550px",

        buttons: [
            {
                id: "BtnCreateNew",
                html: `<i class="fa fa-plus"></i> New Service`,
                className: "form-control form-control-sm w-auto d-inline-flex align-items-center gap-1 bg-white border border-primary text-primary shadow-none",
                onClick: function () {
                    openCreateModal();
                }
            }
        ],

        columnDefs: [
            {
                field: "name",
                headerName: "Name",
                flex: 2
            },
            {
                field: "description",
                headerName: "Description",
                flex: 3,
                valueFormatter: params => params.value || '-'
            },
            {
                field: "isActive",
                headerName: "Status",
                flex: 1,
                filter: false,
                sortable: false,
                cellRenderer: function (params) {
                    if (!params.data) return '';
                    return params.value
                        ? `<span class="badge bg-success">Active</span>`
                        : `<span class="badge bg-secondary">Inactive</span>`;
                }
            },
            {
                headerName: "Actions",
                flex: 1,
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

        // Client-side loading
        rowData: function () {
            return $.ajax({
                url: window.serviceUrls.list,
                type: 'GET',
                data: { start: 0, length: 1000 }
            }).then(response => response.data || response.items || []);
        }
    });


    // =========================================================
    // EVENT HANDLERS
    // =========================================================

    // ---------- OPEN CREATE MODAL ----------
    function openCreateModal() {
        $('#ServiceForm')[0].reset();
        $('#ServiceId').val('');
        $('#IsActive').prop('checked', true);
        $('#ModalTitle').text('New Service');
        $('#ServiceModal').modal('show');
    }


    // ---------- OPEN EDIT MODAL ----------
    $(document).on('click', '#ServicesGrid .edit-button', function () {
        const rawData = $(this).attr('data-row');
        if (!rawData) return;

        const data = JSON.parse(decodeURIComponent(rawData));

        $('#ServiceId').val(data.id || data.Id || '');
        $('#Name').val(data.name || data.Name || '');
        $('#Description').val(data.description || data.Description || '');
        $('#IsActive').prop('checked', Boolean(data.isActive ?? data.IsActive));

        $('#ModalTitle').text('Edit Service');
        $('#ServiceModal').modal('show');
    });


    // ---------- SAVE (CREATE + EDIT) ----------
    $('#ServiceForm').on('submit', function (e) {
        e.preventDefault();

        const $form = $(this);
        const id = $('#ServiceId').val();
        const isEdit = !!id;

        const name = $('#Name').val();
        if (!name || name.trim() === '') {
            abp.notify.error('Please enter a Service name.');
            return;
        }

        const $btn = $form.find('button[type="submit"]');
        $btn.prop('disabled', true);

        let url = isEdit
            ? `${window.serviceUrls.update}&id=${id}`
            : window.serviceUrls.create;

        $.ajax({
            url: url,
            type: 'POST',
            data: $form.serialize(),
            headers: {
                "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                if (res.success) {
                    $('#ServiceModal').modal('hide');
                    window.serviceGrid.refresh();
                    abp.notify.success(res.message || (isEdit ? "Updated successfully" : "Created successfully"));
                } else {
                    abp.notify.error(res.message || "An error occurred");
                }
            },
            error: function () {
                abp.notify.error("An error occurred while saving.");
            },
            complete: function () {
                $btn.prop('disabled', false);
            }
        });
    });


    // ---------- DELETE ----------
    $(document).on('click', '#ServicesGrid .delete-button', function () {
        const id = $(this).data('id');

        abp.message.confirm('Are you sure you want to delete this service?', 'Confirm Deletion')
            .then(function (isConfirmed) {
                if (!isConfirmed) return;

                $.ajax({
                    url: `${window.serviceUrls.delete}?id=${id}`,
                    type: 'POST',
                    headers: {
                        "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function (res) {
                        if (res.success) {
                            window.serviceGrid.refresh();
                            abp.notify.success("Deleted successfully");
                        } else {
                            abp.notify.error(res.message || "Failed to delete");
                        }
                    },
                    error: function () {
                        abp.notify.error("Failed to delete Service");
                    }
                });
            });
    });

});