(function () {
    'use strict';

    if (window.__crmMessengerLoaded) return;
    window.__crmMessengerLoaded = true;

    $(function () {
        if (!window.abp || !abp.currentUser ||
            !abp.currentUser.isAuthenticated) return;

        var BASE = '/Messenger';
        var MAX_FILE = 25 * 1024 * 1024;
        var NOTE_DEFAULT = 'Press Enter to send · Shift + Enter for a new line';

        var EMOJIS = Array.from(
            '😀😃😄😁😆😅😂🤣🥲😊😇🙂🙃😉😌😍🥰😘😗😙😚😋😛😜🤪😝🤑🤗🤭🤫🤔🤐😐😑😶😏😒🙄😬😮‍💨😌😔😪😴😷🤒🤕🤢🤮🤧🥵🥶😵🤯🤠🥳😎🤓😕😟🙁☹️😮😯😲😳🥺😦😧😨😰😥😢😭😱😖😣😞😓😩😫🥱😤😡😠🤬😈👿💀💩🤡👻👽🤖' +
            '👍👎👌✌️🤞🤟🤘🤙👈👉👆👇☝️✋🤚🖐️👋👏🙌👐🤲🤝🙏💪🫶' +
            '❤️🧡💛💚💙💜🖤🤍🤎💔❣️💕💞💓💗💖💘💝💯💢💥💫💦💨🔥✨⭐🌟🎉🎊🎁🏆✅❌⚠️❓❗📌📞📧💰🛒📦🚚'
        );

        var state = {
            open: false,
            convId: null,
            convName: '',
            lastSig: '',
            timer: null,
            uploading: false
        };

        // Remove old UI if script is reinitialized.
        $('#crmMsgFab, #crmMsgPanel, #crmMsgLightbox').remove();

        // Floating Messenger button.
        var $fab = $(
            '<button id="crmMsgFab" title="Open Messenger Inbox">' +
            '<i class="fa-brands fa-facebook-messenger"></i>' +
            '<span id="crmMsgBadge"></span>' +
            '</button>'
        );

        var $lightbox = $(
            '<div id="crmMsgLightbox"><img alt=""></div>'
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

            // Emoji picker
            '<div id="crmMsgEmoji"><div class="grid"></div></div>' +

            '<div id="crmMsgFoot" class="crm-msg-foot">' +
            '<button type="button" id="crmMsgAttach" class="crm-msg-tool" title="Attach files">' +
            '<i class="fa fa-paperclip"></i>' +
            '</button>' +
            '<button type="button" id="crmMsgEmojiBtn" class="crm-msg-tool" title="Emoji">' +
            '<i class="fa-regular fa-face-smile"></i>' +
            '</button>' +
            '<input type="file" id="crmMsgFile" multiple style="display:none">' +
            '<textarea id="crmMsgInput" ' +
            'placeholder="Write a message..." ' +
            'rows="1"></textarea>' +
            '<button id="crmMsgSend" title="Send message">' +
            '<i class="fa fa-paper-plane"></i>' +
            '</button>' +
            '</div>' +

            '<div class="crm-msg-footer-note" id="crmMsgNote">' +
            NOTE_DEFAULT +
            '</div>' +

            '</div>' +
            '</main>' +

            '</div>' +
            '</section>'
        );

        $('body').append($fab, $panel, $lightbox);

        // Build emoji grid.
        (function () {
            var html = '';
            EMOJIS.forEach(function (e) {
                html += '<button type="button" data-e="' + e + '">' + e + '</button>';
            });
            $('#crmMsgEmoji .grid').html(html);
        })();

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

        function fmtSize(bytes) {
            if (!bytes) return '';
            if (bytes < 1024) return bytes + ' B';
            if (bytes < 1048576) return Math.round(bytes / 1024) + ' KB';
            return (bytes / 1048576).toFixed(1) + ' MB';
        }

        function errMsg(e) {
            if (!e) return 'Something went wrong.';
            if (e.error && e.error.message) return e.error.message;
            if (e.message) return e.message;
            if (e.responseJSON && e.responseJSON.error && e.responseJSON.error.message) {
                return e.responseJSON.error.message;
            }
            return 'Something went wrong.';
        }

        // True when the text is only emoji (shown big, like Messenger).
        function isEmojiOnly(text) {
            if (!text) return false;
            var t = text.trim();
            if (!t || Array.from(t).length > 12) return false;
            try {
                return /^(?:\p{Extended_Pictographic}|\u200d|\ufe0f|\s)+$/u.test(t);
            } catch (e) {
                return false;
            }
        }

        function call(type, url, data) {
            return abp.ajax({
                url: BASE + url,
                type: type,
                abpHandleError: false,
                data: data ? JSON.stringify(data) : undefined
            });
        }

        function getToken() {
            try {
                if (abp.security && abp.security.antiForgery &&
                    abp.security.antiForgery.getToken) {
                    return abp.security.antiForgery.getToken();
                }
            } catch (e) { }
            return $('input[name="__RequestVerificationToken"]').first().val() || '';
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

        function attachmentHtml(m) {
            var url = BASE + '?handler=File&id=' + encodeURIComponent(m.id);

            switch (m.attachmentType) {
                case 'image':
                    return '<img class="crm-att-img" src="' + url +
                        '" data-full="' + url + '" alt="' + esc(m.attachmentName) + '">';
                case 'video':
                    return '<video class="crm-att-video" src="' + url +
                        '" controls preload="metadata"></video>';
                case 'audio':
                    return '<audio class="crm-att-audio" src="' + url +
                        '" controls preload="none"></audio>';
                default:
                    return '<a class="crm-att-file" href="' + url +
                        '&download=true" download>' +
                        '<i class="fa fa-file-lines"></i>' +
                        '<span><span class="nm">' + esc(m.attachmentName || 'File') + '</span>' +
                        '<span class="sz">' + esc(fmtSize(m.attachmentSize)) + '</span></span>' +
                        '</a>';
            }
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
            $('#crmMsgEmoji').removeClass('open');

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
            $('#crmMsgEmoji').removeClass('open');

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
                    var stick = forceScroll || nearBottom;

                    var html = '<div class="crm-msg-thread">';

                    messages.forEach(function (m) {
                        var hasAtt = !!m.attachmentType;
                        var cls = 'crm-bubble ' + (m.isFromCustomer ? 'in' : 'out');

                        if (hasAtt) cls += ' media';
                        else if (isEmojiOnly(m.text)) cls += ' emoji-only';

                        html +=
                            '<div class="' + cls + '">' +
                            (hasAtt ? attachmentHtml(m) : '') +
                            (m.text
                                ? '<div class="crm-bubble-text">' + esc(m.text) + '</div>'
                                : '') +
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

                    if (stick) {
                        body.scrollTop = body.scrollHeight;

                        // Images finish loading after render, keep the view at the bottom.
                        $('#crmMsgBody img.crm-att-img').on('load', function () {
                            body.scrollTop = body.scrollHeight;
                        });
                    }

                    call('POST', '?handler=Read&id=' + encodeURIComponent(id))
                        .then(loadUnread);
                });
        }

        // Send text reply.
        function sendMessage() {
            var $input = $('#crmMsgInput');
            var text = $.trim($input.val());

            if (!text || !state.convId || state.uploading) return;

            var id = state.convId;
            var $button = $('#crmMsgSend');

            $button.prop('disabled', true);

            call('POST',
                '?handler=Send&id=' + encodeURIComponent(id),
                { text: text }
            ).then(function () {
                if (String(state.convId) !== String(id)) return;

                $input.val('');
                $('#crmMsgEmoji').removeClass('open');
                refreshChat(true);
                refreshList();
            }).fail(function (e) {
                abp.notify.error(errMsg(e));
            }).always(function () {
                $button.prop('disabled', false);
                $input.trigger('focus');
            });
        }

        // Upload one file.
        function uploadOne(file) {
            var fd = new FormData();
            fd.append('file', file, file.name || 'file');

            var token = getToken();
            if (token) fd.append('__RequestVerificationToken', token);

            return $.ajax({
                url: BASE + '?handler=SendFile&id=' + encodeURIComponent(state.convId),
                type: 'POST',
                data: fd,
                processData: false,
                contentType: false,
                headers: token ? { RequestVerificationToken: token } : {}
            });
        }

        // Upload several files, one after another.
        async function uploadFiles(fileList) {
            var files = Array.from(fileList || []);
            if (!files.length || !state.convId || state.uploading) return;

            var id = state.convId;
            state.uploading = true;
            $('#crmMsgSend, #crmMsgAttach').prop('disabled', true);

            for (var i = 0; i < files.length; i++) {
                var f = files[i];

                if (f.size > MAX_FILE) {
                    abp.notify.error('"' + f.name + '" is larger than 25 MB.');
                    continue;
                }

                $('#crmMsgNote').text('Uploading ' + (i + 1) + ' of ' + files.length + ': ' + f.name + ' ...');

                try {
                    await uploadOne(f);
                    if (String(state.convId) === String(id)) refreshChat(true);
                } catch (e) {
                    abp.notify.error(errMsg(e));
                }
            }

            state.uploading = false;
            $('#crmMsgSend, #crmMsgAttach').prop('disabled', false);
            $('#crmMsgNote').text(NOTE_DEFAULT);
            $('#crmMsgFile').val('');
            refreshList();
        }

        function insertAtCursor(el, text) {
            var start = el.selectionStart == null ? el.value.length : el.selectionStart;
            var end = el.selectionEnd == null ? el.value.length : el.selectionEnd;

            el.value = el.value.slice(0, start) + text + el.value.slice(end);
            el.selectionStart = el.selectionEnd = start + text.length;
            el.focus();
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

        // Attach button + file input.
        $('#crmMsgAttach').on('click', function () {
            $('#crmMsgFile').trigger('click');
        });

        $('#crmMsgFile').on('change', function () {
            uploadFiles(this.files);
        });

        // Paste a screenshot / copied file with Ctrl+V.
        $('#crmMsgInput').on('paste', function (e) {
            var cd = e.originalEvent && e.originalEvent.clipboardData;
            if (cd && cd.files && cd.files.length) {
                e.preventDefault();
                uploadFiles(cd.files);
            }
        });

        // Drag and drop files onto the chat.
        $('#crmMsgChat')
            .on('dragover', function (e) {
                e.preventDefault();
                $(this).addClass('drag');
            })
            .on('dragleave drop', function () {
                $(this).removeClass('drag');
            })
            .on('drop', function (e) {
                e.preventDefault();
                var dt = e.originalEvent && e.originalEvent.dataTransfer;
                if (dt && dt.files && dt.files.length) uploadFiles(dt.files);
            });

        // Emoji picker.
        $('#crmMsgEmojiBtn').on('click', function (e) {
            e.stopPropagation();
            $('#crmMsgEmoji').toggleClass('open');
        });

        $('#crmMsgEmoji').on('click', 'button', function (e) {
            e.stopPropagation();
            insertAtCursor(document.getElementById('crmMsgInput'), $(this).attr('data-e'));
        });

        $(document).on('click', function (e) {
            if (!$(e.target).closest('#crmMsgEmoji, #crmMsgEmojiBtn').length) {
                $('#crmMsgEmoji').removeClass('open');
            }
        });

        // Image viewer (stays inside your software).
        $('#crmMsgBody').on('click', '.crm-att-img', function () {
            $lightbox.find('img').attr('src', $(this).attr('data-full'));
            $lightbox.addClass('open');
        });

        $lightbox.on('click', function () {
            $lightbox.removeClass('open').find('img').attr('src', '');
        });

        $(document).on('keydown', function (e) {
            if (e.key === 'Escape') {
                $lightbox.removeClass('open');
                $('#crmMsgEmoji').removeClass('open');
            }
        });

        // Background unread badge.
        loadUnread();

        setInterval(function () {
            if (!state.open) loadUnread();
        }, 20000);
    });
})();