(function () {
    'use strict';

    if (window.__crmMessengerLoaded) return;
    window.__crmMessengerLoaded = true;

    $(function () {
        if (!window.abp || !abp.currentUser ||
            !abp.currentUser.isAuthenticated) return;

        var BASE = '/Messenger';

        var state = {
            open: false,
            convId: null,
            convName: '',
            lastSig: '',
            timer: null
        };

        // Remove old UI if script is reinitialized.
        $('#crmMsgFab, #crmMsgPanel').remove();

        // Floating Messenger button.
        var $fab = $(
            '<button id="crmMsgFab" title="Open Messenger Inbox">' +
            '<i class="fa-brands fa-facebook-messenger"></i>' +
            '<span id="crmMsgBadge"></span>' +
            '</button>'
        );

        // Full two-column Messenger window.
        var $panel = $(
            '<section id="crmMsgPanel" aria-label="Messenger Inbox">' +

            '<header class="crm-msg-topbar">' +
            '<div class="crm-msg-brand">' +
            '<span class="crm-msg-brand-icon">' +
            '<i class="fa-brands fa-facebook-messenger"></i>' +
            '</span>' +
            '<div>' +
            '<div class="crm-msg-brand-title">Messenger</div>' +
            '<div class="crm-msg-brand-subtitle">CRM Inbox</div>' +
            '</div>' +
            '</div>' +
            '<button id="crmMsgClose" title="Close Messenger">' +
            '<i class="fa fa-times"></i>' +
            '</button>' +
            '</header>' +

            '<div class="crm-msg-layout">' +

            // LEFT: Inbox sidebar.
            '<aside class="crm-msg-sidebar">' +
            '<div class="crm-msg-sidebar-head">' +
            '<div class="crm-msg-sidebar-title">Chats</div>' +
            '<span id="crmMsgConversationCount">0</span>' +
            '</div>' +

            '<div class="crm-msg-search">' +
            '<i class="fa fa-search"></i>' +
            '<input id="crmMsgSearch" type="text" ' +
            'placeholder="Search conversations..." ' +
            'autocomplete="off">' +
            '</div>' +

            '<div id="crmMsgList" class="crm-msg-list">' +
            '<div class="crm-msg-loading">Loading conversations...</div>' +
            '</div>' +
            '</aside>' +

            // RIGHT: Conversation panel.
            '<main class="crm-msg-main">' +

            '<div id="crmMsgWelcome" class="crm-msg-welcome">' +
            '<div class="crm-msg-welcome-icon">' +
            '<i class="fa-brands fa-facebook-messenger"></i>' +
            '</div>' +
            '<h3>Your messages</h3>' +
            '<p>Select a conversation to view messages and reply to customers.</p>' +
            '</div>' +

            '<div id="crmMsgChat" class="crm-msg-chat">' +

            '<div class="crm-msg-chat-head">' +
            '<button id="crmMsgBack" title="Back to inbox">' +
            '<i class="fa fa-arrow-left"></i>' +
            '</button>' +

            '<div id="crmMsgChatAvatar" class="crm-msg-chat-avatar">?</div>' +

            '<div class="crm-msg-chat-info">' +
            '<div id="crmMsgTitle" class="crm-msg-chat-name">' +
            'Customer' +
            '</div>' +
            '<div class="crm-msg-chat-status">' +
            '<span class="crm-msg-status-dot"></span>' +
            '<span>Messenger conversation</span>' +
            '</div>' +
            '</div>' +
            '</div>' +

            '<div id="crmMsgBody" class="crm-msg-body">' +
            '</div>' +

            '<div id="crmMsgFoot" class="crm-msg-foot">' +
            '<textarea id="crmMsgInput" ' +
            'placeholder="Write a message..." ' +
            'rows="1"></textarea>' +
            '<button id="crmMsgSend" title="Send message">' +
            '<i class="fa fa-paper-plane"></i>' +
            '</button>' +
            '</div>' +

            '<div class="crm-msg-footer-note">' +
            'Press Enter to send · Shift + Enter for a new line' +
            '</div>' +

            '</div>' +
            '</main>' +

            '</div>' +
            '</section>'
        );

        $('body').append($fab, $panel);

        // Helpers.
        function esc(value) {
            return $('<div>').text(value == null ? '' : value).html();
        }

        function toDate(value) {
            if (!value) return new Date();

            return new Date(
                /Z|[+-]\d\d:?\d\d$/.test(value)
                    ? value
                    : value + 'Z'
            );
        }

        function fmtTime(value) {
            var d = toDate(value);

            if (isNaN(d.getTime())) return '';

            var now = new Date();

            if (d.toDateString() === now.toDateString()) {
                return d.toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit'
                });
            }

            return d.toLocaleDateString([], {
                month: 'short',
                day: 'numeric'
            });
        }

        function call(type, url, data) {
            return abp.ajax({
                url: BASE + url,
                type: type,
                abpHandleError: false,
                data: data ? JSON.stringify(data) : undefined
            });
        }

        function setBadge(count) {
            var $badge = $('#crmMsgBadge');
            count = Number(count) || 0;

            if (count > 0) {
                $badge.text(count > 99 ? '99+' : count).show();
            } else {
                $badge.hide();
            }
        }

        function getInitial(name) {
            return (name || '?').trim().charAt(0).toUpperCase() || '?';
        }

        function getAvatar(name, url, extraClass) {
            if (url) {
                return '<img class="' + extraClass +
                    '" src="' + esc(url) +
                    '" alt="' + esc(name) + '">';
            }

            return '<div class="' + extraClass + '">' +
                esc(getInitial(name)) +
                '</div>';
        }

        // Unread count.
        function loadUnread() {
            call('GET', '?handler=Unread')
                .then(function (result) {
                    setBadge(result.count);
                });
        }

        // Open / close the full Messenger window.
        function openMessenger() {
            state.open = true;

            $panel.addClass('open');
            $fab.addClass('active');

            refreshList();
            loadUnread();
            startTimer();
        }

        function closeMessenger() {
            state.open = false;

            $panel.removeClass('open');
            $fab.removeClass('active');

            stopTimer();
            loadUnread();
        }

        // Inbox list stays visible on desktop.
        function refreshList() {
            call('GET', '?handler=Conversations')
                .then(function (list) {
                    list = list || [];

                    var totalUnread = 0;
                    var html = '';

                    $('#crmMsgConversationCount').text(list.length);

                    list.forEach(function (c) {
                        totalUnread += Number(c.unreadCount) || 0;

                        var selected =
                            state.convId != null &&
                            String(state.convId) === String(c.id);

                        html +=
                            '<button type="button" class="crm-msg-item ' +
                            (selected ? 'selected' : '') + '" ' +
                            'data-id="' + esc(c.id) + '" ' +
                            'data-name="' + esc(c.customerName) + '">' +

                            getAvatar(
                                c.customerName,
                                c.profilePictureUrl,
                                'crm-msg-avatar'
                            ) +

                            '<span class="crm-msg-meta">' +
                            '<span class="crm-msg-item-top">' +
                            '<span class="crm-msg-item-name">' +
                            esc(c.customerName || 'Unknown customer') +
                            '</span>' +
                            '<span class="crm-msg-time">' +
                            esc(fmtTime(c.lastMessageTime)) +
                            '</span>' +
                            '</span>' +

                            '<span class="crm-msg-item-bottom">' +
                            '<span class="crm-msg-last">' +
                            esc(c.lastMessageText || 'No messages yet') +
                            '</span>' +

                            (c.unreadCount > 0
                                ? '<span class="crm-msg-unread">' +
                                esc(c.unreadCount) +
                                '</span>'
                                : '') +
                            '</span>' +
                            '</span>' +
                            '</button>';
                    });

                    $('#crmMsgList').html(
                        html ||
                        '<div class="crm-msg-empty-list">' +
                        '<i class="fa-regular fa-comments"></i>' +
                        '<p>No conversations yet</p>' +
                        '<small>Messages from your connected page will appear here.</small>' +
                        '</div>'
                    );

                    filterConversations();
                    setBadge(totalUnread);
                });
        }

        // Search conversations by customer name or last message.
        function filterConversations() {
            var query = ($('#crmMsgSearch').val() || '')
                .trim()
                .toLowerCase();

            $('#crmMsgList .crm-msg-item').each(function () {
                var text = $(this).text().toLowerCase();

                $(this).toggle(!query || text.indexOf(query) !== -1);
            });
        }

        // Select a conversation.
        function openChat(id, name) {
            state.convId = id;
            state.convName = name || 'Customer';
            state.lastSig = '';

            $('#crmMsgTitle').text(state.convName);

            var selected = $('#crmMsgList .crm-msg-item').filter(function () {
                return String($(this).attr('data-id')) === String(id);
            }).first();

            var avatarUrl = selected.find('.crm-msg-avatar').is('img')
                ? selected.find('.crm-msg-avatar').attr('src')
                : '';

            $('#crmMsgChatAvatar').empty().append(
                getAvatar(
                    state.convName,
                    avatarUrl,
                    'crm-msg-chat-avatar'
                )
            );

            $('#crmMsgWelcome').hide();
            $('#crmMsgChat').addClass('active');
            $('#crmMsgList .crm-msg-item').removeClass('selected');

            $('#crmMsgList .crm-msg-item').filter(function () {
                return String($(this).attr('data-id')) === String(id);
            }).addClass('selected');

            // On mobile, show chat instead of sidebar.
            $panel.addClass('mobile-chat');

            $('#crmMsgBody').html(
                '<div class="crm-msg-chat-loading">' +
                '<span class="crm-msg-spinner"></span>' +
                '<p>Loading messages...</p>' +
                '</div>'
            );

            refreshChat(true);

            call('POST', '?handler=Read&id=' + encodeURIComponent(id))
                .then(function () {
                    loadUnread();
                    refreshList();
                });

            $('#crmMsgInput').trigger('focus');
        }

        // Back button is mainly for mobile screens.
        function backToInbox() {
            $panel.removeClass('mobile-chat');
            $('#crmMsgWelcome').show();
            $('#crmMsgChat').removeClass('active');
        }

        // Load conversation messages.
        function refreshChat(forceScroll) {
            if (!state.convId) return;

            var id = state.convId;

            call('GET', '?handler=Messages&id=' + encodeURIComponent(id))
                .then(function (messages) {
                    if (
                        !state.open ||
                        !state.convId ||
                        String(state.convId) !== String(id)
                    ) return;

                    messages = messages || [];

                    var signature = messages.map(function (m) {
                        return m.id + ':' + m.text;
                    }).join('|');

                    if (signature === state.lastSig && !forceScroll) return;

                    state.lastSig = signature;

                    var body = document.getElementById('crmMsgBody');
                    var nearBottom =
                        body.scrollHeight - body.scrollTop - body.clientHeight < 100;

                    var html = '<div class="crm-msg-thread">';

                    messages.forEach(function (m) {
                        html +=
                            '<div class="crm-bubble ' +
                            (m.isFromCustomer ? 'in' : 'out') + '">' +
                            '<div class="crm-bubble-text">' +
                            esc(m.text) +
                            '</div>' +
                            '<small>' + esc(fmtTime(m.sentTime)) + '</small>' +
                            '</div>';
                    });

                    html += '</div>';

                    $('#crmMsgBody').html(
                        messages.length
                            ? html
                            : '<div class="crm-msg-no-messages">' +
                            '<i class="fa-regular fa-comments"></i>' +
                            '<p>Start the conversation</p>' +
                            '<small>Send a message to this customer below.</small>' +
                            '</div>'
                    );

                    if (forceScroll || nearBottom) {
                        body.scrollTop = body.scrollHeight;
                    }

                    call('POST', '?handler=Read&id=' + encodeURIComponent(id))
                        .then(loadUnread);
                });
        }

        // Send reply.
        function sendMessage() {
            var $input = $('#crmMsgInput');
            var text = $.trim($input.val());

            if (!text || !state.convId) return;

            var id = state.convId;
            var $button = $('#crmMsgSend');

            $button.prop('disabled', true);

            call('POST',
                '?handler=Send&id=' + encodeURIComponent(id),
                { text: text }
            ).then(function () {
                if (String(state.convId) !== String(id)) return;

                $input.val('');
                refreshChat(true);
                refreshList();
            }).always(function () {
                $button.prop('disabled', false);
                $input.trigger('focus');
            });
        }

        // Polling.
        function startTimer() {
            stopTimer();

            state.timer = setInterval(function () {
                if (!state.open) return;

                refreshList();

                if (state.convId) {
                    refreshChat(false);
                }
            }, 4000);
        }

        function stopTimer() {
            if (state.timer) {
                clearInterval(state.timer);
                state.timer = null;
            }
        }

        // Events.
        $fab.on('click', function () {
            if (state.open) {
                closeMessenger();
            } else {
                openMessenger();
            }
        });

        $('#crmMsgClose').on('click', closeMessenger);

        $('#crmMsgBack').on('click', backToInbox);

        $('#crmMsgSearch').on('input', filterConversations);

        $('#crmMsgList').on('click', '.crm-msg-item', function () {
            openChat(
                $(this).attr('data-id'),
                $(this).attr('data-name')
            );
        });

        $('#crmMsgSend').on('click', sendMessage);

        $('#crmMsgInput').on('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        // Background unread badge.
        loadUnread();

        setInterval(function () {
            if (!state.open) loadUnread();
        }, 20000);
    });
})();