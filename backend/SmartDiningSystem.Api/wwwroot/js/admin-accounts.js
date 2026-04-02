(function () {
    const filterForm = document.getElementById('accountsFilterForm');
    const searchInput = document.getElementById('accountsSearch');
    const roleInput = document.getElementById('accountsRole');
    const openCreateButton = document.getElementById('openCreateAccountModal');
    const tableBody = document.getElementById('accountsTableBody');
    const loadingState = document.getElementById('accountsLoadingState');
    const alertHost = document.getElementById('accountsAlertHost');
    const formAlertHost = document.getElementById('accountFormAlertHost');
    const form = document.getElementById('accountEditorForm');
    const saveButton = document.getElementById('saveAccountButton');
    const restaurantForm = document.getElementById('restaurantEditorForm');
    const restaurantFormAlertHost = document.getElementById('restaurantFormAlertHost');
    const saveRestaurantButton = document.getElementById('saveRestaurantButton');
    const roleField = document.getElementById('role');
    const modalLabel = document.getElementById('accountEditorModalLabel');
    const modalHint = document.getElementById('accountEditorModalHint');
    const accountIdField = document.getElementById('accountId');
    const updatePasswordHint = document.getElementById('updatePasswordHint');
    const createOnlyFields = Array.from(document.querySelectorAll('.create-only-field'));
    const restaurantFields = {
        restaurantName: document.getElementById('restaurantName'),
        restaurantAddress: document.getElementById('restaurantAddress'),
        restaurantDescription: document.getElementById('restaurantDescription')
    };

    if (!filterForm || !form || !restaurantForm || !window.bootstrap) {
        return;
    }

    const editorModal = new bootstrap.Modal(document.getElementById('accountEditorModal'));
    const restaurantModal = new bootstrap.Modal(document.getElementById('restaurantEditorModal'));
    let currentMode = 'create';
    let currentAccounts = [];

    filterForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        await loadAccounts();
    });

    openCreateButton.addEventListener('click', function () {
        openCreateModal();
    });

    saveButton.addEventListener('click', async function () {
        await submitAccount();
    });
    saveRestaurantButton.addEventListener('click', async function () {
        await submitRestaurant();
    });

    roleField.addEventListener('change', refreshRoleDrivenHints);
    searchInput.addEventListener('input', handleAutoResetState);
    roleInput.addEventListener('change', handleAutoResetState);

    tableBody.addEventListener('click', async function (event) {
        const editButton = event.target.closest('[data-action="edit"]');
        if (editButton) {
            const accountId = editButton.getAttribute('data-account-id');
            const account = currentAccounts.find(function (item) { return item.id === accountId; });
            if (account) {
                openEditModal(account);
            }
            return;
        }

        const deleteButton = event.target.closest('[data-action="delete"]');
        if (deleteButton) {
            const accountId = deleteButton.getAttribute('data-account-id');
            const accountName = deleteButton.getAttribute('data-account-name') || 'this account';
            await deleteAccount(accountId, accountName);
            return;
        }

        const addRestaurantButton = event.target.closest('[data-action="add-restaurant"]');
        if (addRestaurantButton) {
            openRestaurantModal(
                addRestaurantButton.getAttribute('data-account-id'),
                addRestaurantButton.getAttribute('data-account-name') || 'this owner');
        }
    });

    void loadAccounts();

    async function loadAccounts() {
        setLoading(true);
        clearAlert(alertHost);

        try {
            const query = new URLSearchParams();
            if (searchInput.value.trim()) {
                query.set('search', searchInput.value.trim());
            }
            if (roleInput.value) {
                query.set('role', roleInput.value);
            }

            const response = await fetch('/api/accounts' + (query.toString() ? '?' + query.toString() : ''), {
                method: 'GET',
                credentials: 'same-origin'
            });

            const payload = await response.json();
            if (!response.ok) {
                throw buildApiError(payload, 'Accounts could not be loaded.');
            }

            currentAccounts = Array.isArray(payload.data) ? payload.data : [];
            renderAccounts();
        } catch (error) {
            currentAccounts = [];
            renderAccounts();
            showAlert(alertHost, 'danger', extractErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    async function handleAutoResetState() {
        if (isDefaultFilterState()) {
            await loadAccounts();
        }
    }

    function renderAccounts() {
        if (!currentAccounts.length) {
            tableBody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-5">No accounts matched the current filters.</td></tr>';
            return;
        }

        tableBody.innerHTML = currentAccounts.map(function (account) {
            return [
                '<tr>',
                '<td><div class="fw-semibold">' + escapeHtml(account.fullName) + '</div></td>',
                '<td>' + escapeHtml(account.username) + '</td>',
                '<td>' + escapeHtml(account.phoneNumber || '-') + '</td>',
                '<td><span class="badge text-bg-light border">' + escapeHtml(account.role) + '</span></td>',
                '<td>' + buildStatusBadge(account.isActive ? 'Active' : 'Inactive', account.isActive ? 'success' : 'secondary') + '</td>',
                '<td>' + buildStatusBadge(account.isPhoneVerified ? 'Verified' : 'Unverified', account.isPhoneVerified ? 'primary' : 'warning') + '</td>',
                '<td>' + buildRestaurantsSummary(account) + '</td>',
                '<td>' + escapeHtml(formatDate(account.createdAtUtc)) + '</td>',
                '<td class="text-end"><div class="d-inline-flex gap-2 admin-row-actions">' +
                    '<button type="button" class="btn btn-sm btn-outline-primary" data-action="edit" data-account-id="' + escapeHtml(account.id) + '">Edit</button>' +
                    (account.role === 'RestaurantOwner'
                        ? '<button type="button" class="btn btn-sm btn-outline-success" data-action="add-restaurant" data-account-id="' + escapeHtml(account.id) + '" data-account-name="' + escapeHtml(account.fullName) + '">Add Restaurant</button>'
                        : '') +
                    '<button type="button" class="btn btn-sm btn-outline-danger" data-action="delete" data-account-id="' + escapeHtml(account.id) + '" data-account-name="' + escapeHtml(account.fullName) + '">Delete</button>' +
                '</div></td>',
                '</tr>'
            ].join('');
        }).join('');
    }

    function openCreateModal() {
        currentMode = 'create';
        form.reset();
        clearErrors();
        clearAlert(formAlertHost);
        accountIdField.value = '';
        document.getElementById('isActive').checked = true;
        document.getElementById('isPhoneVerified').checked = true;
        modalLabel.textContent = 'Create Account';
        modalHint.textContent = 'Submit through POST /api/accounts.';
        createOnlyFields.forEach(function (field) { field.classList.remove('d-none'); });
        updatePasswordHint.classList.add('d-none');
        refreshRoleDrivenHints();
        editorModal.show();
    }

    function openEditModal(account) {
        currentMode = 'edit';
        form.reset();
        clearErrors();
        clearAlert(formAlertHost);
        accountIdField.value = account.id;
        document.getElementById('fullName').value = account.fullName || '';
        document.getElementById('username').value = account.username || '';
        document.getElementById('phoneNumber').value = account.phoneNumber || '';
        document.getElementById('role').value = account.role || 'User';
        document.getElementById('isActive').checked = Boolean(account.isActive);
        document.getElementById('isPhoneVerified').checked = Boolean(account.isPhoneVerified);
        modalLabel.textContent = 'Edit Account';
        modalHint.textContent = 'Submit through PUT /api/accounts/{id}. Leave restaurant fields blank to keep current owner details unchanged.';
        createOnlyFields.forEach(function (field) { field.classList.add('d-none'); });
        updatePasswordHint.classList.remove('d-none');
        refreshRoleDrivenHints();
        editorModal.show();
    }

    async function submitAccount() {
        saveButton.disabled = true;
        clearErrors();
        clearAlert(formAlertHost);

        try {
            const payload = buildPayload();
            const isCreate = currentMode === 'create';
            const url = isCreate ? '/api/accounts' : '/api/accounts/' + encodeURIComponent(accountIdField.value);
            const method = isCreate ? 'POST' : 'PUT';

            const response = await fetch(url, {
                method: method,
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();
            if (!response.ok) {
                if (result && result.errors) {
                    applyFieldErrors(result.errors);
                }
                throw buildApiError(result, isCreate ? 'Account could not be created.' : 'Account could not be updated.');
            }

            editorModal.hide();
            showAlert(alertHost, 'success', result.message || (isCreate ? 'Account created successfully.' : 'Account updated successfully.'));
            await loadAccounts();
        } catch (error) {
            showAlert(formAlertHost, 'danger', extractErrorMessage(error));
        } finally {
            saveButton.disabled = false;
        }
    }

    async function deleteAccount(accountId, accountName) {
        if (!window.confirm('Delete ' + accountName + '? This will call DELETE /api/accounts/{id}.')) {
            return;
        }

        clearAlert(alertHost);

        try {
            const response = await fetch('/api/accounts/' + encodeURIComponent(accountId), {
                method: 'DELETE',
                credentials: 'same-origin'
            });
            const result = await response.json();
            if (!response.ok) {
                throw buildApiError(result, 'Account could not be deleted.');
            }

            showAlert(alertHost, 'success', result.message || 'Account deleted successfully.');
            await loadAccounts();
        } catch (error) {
            showAlert(alertHost, 'danger', extractErrorMessage(error));
        }
    }

    function openRestaurantModal(ownerUserId, ownerName) {
        restaurantForm.reset();
        clearRestaurantErrors();
        clearAlert(restaurantFormAlertHost);
        document.getElementById('restaurantOwnerUserId').value = ownerUserId || '';
        document.getElementById('restaurantEditorModalLabel').textContent = 'Add Restaurant';
        document.getElementById('restaurantEditorModalHint').textContent = 'Create an additional restaurant for ' + ownerName + '.';
        restaurantModal.show();
    }

    async function submitRestaurant() {
        saveRestaurantButton.disabled = true;
        clearRestaurantErrors();
        clearAlert(restaurantFormAlertHost);

        try {
            const payload = buildRestaurantPayload();
            const response = await fetch('/api/admin/restaurants', {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();
            if (!response.ok) {
                if (result && result.errors) {
                    applyRestaurantFieldErrors(result.errors);
                }
                throw buildApiError(result, 'Restaurant could not be created.');
            }

            restaurantModal.hide();
            showAlert(alertHost, 'success', result.message || 'Restaurant created successfully.');
            await loadAccounts();
        } catch (error) {
            showAlert(restaurantFormAlertHost, 'danger', extractErrorMessage(error));
        } finally {
            saveRestaurantButton.disabled = false;
        }
    }

    function buildPayload() {
        const payload = {
            fullName: document.getElementById('fullName').value.trim(),
            username: document.getElementById('username').value.trim(),
            phoneNumber: document.getElementById('phoneNumber').value.trim(),
            role: document.getElementById('role').value,
            isActive: document.getElementById('isActive').checked,
            isPhoneVerified: document.getElementById('isPhoneVerified').checked
        };

        if (currentMode === 'create') {
            payload.password = document.getElementById('password').value;
            payload.confirmPassword = document.getElementById('confirmPassword').value;
        }

        if (payload.role === 'RestaurantOwner') {
            const restaurantName = restaurantFields.restaurantName.value.trim();
            const restaurantAddress = restaurantFields.restaurantAddress.value.trim();
            const restaurantDescription = restaurantFields.restaurantDescription.value.trim();

            if (currentMode === 'create' || restaurantName) {
                payload.restaurantName = restaurantName;
            }
            if (currentMode === 'create' || restaurantAddress) {
                payload.restaurantAddress = restaurantAddress;
            }
            if (currentMode === 'create' || restaurantDescription) {
                payload.restaurantDescription = restaurantDescription;
            }
        }

        return payload;
    }

    function buildRestaurantPayload() {
        const latitudeValue = document.getElementById('restaurantCreateLatitude').value.trim();
        const longitudeValue = document.getElementById('restaurantCreateLongitude').value.trim();

        return {
            ownerUserId: document.getElementById('restaurantOwnerUserId').value,
            name: document.getElementById('restaurantCreateName').value.trim(),
            description: document.getElementById('restaurantCreateDescription').value.trim(),
            address: document.getElementById('restaurantCreateAddress').value.trim(),
            contactPhone: document.getElementById('restaurantCreatePhone').value.trim(),
            imageUrl: document.getElementById('restaurantCreateImageUrl').value.trim() || null,
            latitude: latitudeValue ? Number(latitudeValue) : null,
            longitude: longitudeValue ? Number(longitudeValue) : null
        };
    }

    function isDefaultFilterState() {
        return !searchInput.value.trim() && !roleInput.value;
    }

    function refreshRoleDrivenHints() {
        const ownerMode = roleField.value === 'RestaurantOwner';
        Object.values(restaurantFields).forEach(function (field) {
            field.closest('.restaurant-owner-field').classList.toggle('opacity-75', !ownerMode);
        });
    }

    function applyFieldErrors(errors) {
        Object.keys(errors).forEach(function (key) {
            const normalized = key.charAt(0).toLowerCase() + key.slice(1);
            const field = form.querySelector('[name="' + normalized + '"]');
            if (!field) {
                return;
            }
            field.classList.add('is-invalid');
            const feedback = field.parentElement.querySelector('.invalid-feedback');
            if (feedback) {
                feedback.textContent = Array.isArray(errors[key]) ? errors[key].join(' ') : String(errors[key]);
            }
        });
    }

    function applyRestaurantFieldErrors(errors) {
        Object.keys(errors).forEach(function (key) {
            const normalized = key.charAt(0).toLowerCase() + key.slice(1);
            const field = restaurantForm.querySelector('[name="' + normalized + '"]');
            if (!field) {
                return;
            }
            field.classList.add('is-invalid');
            const feedback = field.parentElement.querySelector('.invalid-feedback');
            if (feedback) {
                feedback.textContent = Array.isArray(errors[key]) ? errors[key].join(' ') : String(errors[key]);
            }
        });
    }

    function clearErrors() {
        form.querySelectorAll('.is-invalid').forEach(function (field) {
            field.classList.remove('is-invalid');
        });
        form.querySelectorAll('.invalid-feedback').forEach(function (feedback) {
            feedback.textContent = '';
        });
    }

    function clearRestaurantErrors() {
        restaurantForm.querySelectorAll('.is-invalid').forEach(function (field) {
            field.classList.remove('is-invalid');
        });
        restaurantForm.querySelectorAll('.invalid-feedback').forEach(function (feedback) {
            feedback.textContent = '';
        });
    }

    function setLoading(isLoading) {
        loadingState.classList.toggle('d-none', !isLoading);
    }

    function showAlert(host, type, message) {
        host.innerHTML = '<div class="alert alert-' + type + ' shadow-sm" role="alert">' + escapeHtml(message) + '</div>';
    }

    function clearAlert(host) {
        host.innerHTML = '';
    }

    function buildApiError(payload, fallbackMessage) {
        const error = new Error(payload && payload.message ? payload.message : fallbackMessage);
        error.payload = payload;
        return error;
    }

    function extractErrorMessage(error) {
        if (error && error.payload && error.payload.message) {
            return error.payload.message;
        }
        if (error && error.message) {
            return error.message;
        }
        return 'Something went wrong.';
    }

    function buildStatusBadge(label, type) {
        return '<span class="badge text-bg-' + type + '">' + escapeHtml(label) + '</span>';
    }

    function buildRestaurantsSummary(account) {
        const restaurants = Array.isArray(account.ownedRestaurants) ? account.ownedRestaurants : [];
        if (!restaurants.length) {
            return '<span class="text-muted">-</span>';
        }

        return [
            '<div class="small fw-semibold mb-1">' + restaurants.length + ' restaurant' + (restaurants.length === 1 ? '' : 's') + '</div>',
            restaurants.map(function (restaurant) {
                return '<div class="small"><span class="fw-semibold">' + escapeHtml(restaurant.restaurantName) + '</span> ' +
                    '<span class="badge text-bg-light border">' + escapeHtml(restaurant.approvalStatus) + '</span></div>';
            }).join('')
        ].join('');
    }

    function formatDate(value) {
        if (!value) {
            return '-';
        }
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }
        return date.toLocaleString();
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }
})();
