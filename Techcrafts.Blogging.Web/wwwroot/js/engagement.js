$(document).ready(function() {
    $('#form-clap').on('submit', function(e) {
        e.preventDefault();
        var form = $(this);
        var btn = form.find('.btn-clap');
        var url = '/api/engagement/clap';
        
        btn.prop('disabled', true);
        $.post(url, form.serialize(), function(data) {
            if(data.success) {
                form.find('.clap-count').text(data.count);
                btn.addClass('animate-pop');
                setTimeout(function() { btn.removeClass('animate-pop'); }, 300);
            }
            btn.prop('disabled', false);
        });
    });

    $('#form-bookmark').on('submit', function(e) {
        e.preventDefault();
        var form = $(this);
        var url = '/api/engagement/bookmark';
        $.post(url, form.serialize(), function(data) {
            if(data.success) {
                var icon = form.find('i.bi');
                if(data.isBookmarked) {
                    icon.removeClass('bi-bookmark').addClass('bi-bookmark-fill');
                } else {
                    icon.removeClass('bi-bookmark-fill').addClass('bi-bookmark');
                }
            }
        });
    });

    // Comments handling
    // When reply is clicked, move the form under the comment
    $(document).on('click', '.btn-reply', function() {
        var commentId = $(this).data('comment-id');
        var form = $('.comment-form');
        
        // Move form to reply container
        $('#reply-container-' + commentId).append(form).removeClass('d-none');
        form.find('.parent-comment-id').val(commentId);
        form.find('.btn-cancel-reply').removeClass('d-none');
        form.find('textarea').focus();
    });

    // When cancel reply is clicked, move form back to root
    $(document).on('click', '.btn-cancel-reply', function() {
        var form = $('.comment-form');
        $('#root-comment-form-container').append(form);
        form.find('.parent-comment-id').val('');
        $(this).addClass('d-none');
    });

    $(document).on('submit', '.comment-form', function(e) {
        e.preventDefault();
        var form = $(this);
        var url = form.attr('action');
        var btn = form.find('button[type="submit"]');
        var parentId = form.find('.parent-comment-id').val();
        
        btn.prop('disabled', true);
        $.post(url, form.serialize(), function(data) {
            $('.no-comments-msg').remove();
            
            if (parentId) {
                // If it's a reply, append next to the form (which is currently in the reply-container)
                $('#reply-container-' + parentId).after('<div class="mt-4 border-start border-2 border-subtle ps-4">' + data + '</div>');
                // Move form back to top
                $('#root-comment-form-container').append(form);
                form.find('.parent-comment-id').val('');
                form.find('.btn-cancel-reply').addClass('d-none');
                $('#reply-container-' + parentId).addClass('d-none');
            } else {
                // If it's a root comment, append to list
                $('#comments-list').append(data);
            }
            
            form.trigger('reset');
            btn.prop('disabled', false);
        });
    });
});
