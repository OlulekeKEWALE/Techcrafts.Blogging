$(document).ready(function () {
    // Handling Reactions Toggle
    $('.reaction-btn').on('click', function (e) {
        e.preventDefault();
        
        const $btn = $(this);
        const postId = $btn.data('post-id');
        const reactionType = $btn.data('reaction'); // 1 = Like, 2 = Dislike
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/api/engagement/ToggleReaction',
            type: 'POST',
            data: {
                postId: postId,
                reaction: reactionType,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    // Reset both buttons
                    $('.reaction-btn[data-reaction="1"]')
                        .removeClass('active border-primary text-primary')
                        .find('i').removeClass('bi-hand-thumbs-up-fill').addClass('bi-hand-thumbs-up');
                    
                    $('.reaction-btn[data-reaction="2"]')
                        .removeClass('active border-danger text-danger')
                        .find('i').removeClass('bi-hand-thumbs-down-fill').addClass('bi-hand-thumbs-down');

                    // Set active button
                    if (response.userReaction === 1) {
                        $('.reaction-btn[data-reaction="1"]')
                            .addClass('active border-primary text-primary')
                            .find('i').removeClass('bi-hand-thumbs-up').addClass('bi-hand-thumbs-up-fill');
                    } else if (response.userReaction === 2) {
                        $('.reaction-btn[data-reaction="2"]')
                            .addClass('active border-danger text-danger')
                            .find('i').removeClass('bi-hand-thumbs-down').addClass('bi-hand-thumbs-down-fill');
                    }

                    // Update counts
                    $('.like-count').text(response.likesCount);
                    $('.dislike-count').text(response.dislikesCount);
                }
            },
            error: function () {
                console.error("Failed to toggle reaction.");
            }
        });
    });

    // Handling Web Share API / Copy Link
    $('#btn-copy-share').on('click', async function () {
        const $btn = $(this);
        const title = $btn.data('title');
        const url = $btn.data('url');
        const $icon = $btn.find('i');

        if (navigator.share) {
            try {
                await navigator.share({
                    title: title,
                    url: url
                });
            } catch (err) {
                console.log("Share cancelled or failed.", err);
            }
        } else {
            // Fallback: Copy to clipboard
            navigator.clipboard.writeText(url).then(function () {
                // Show temporary checkmark
                $icon.removeClass('bi-link-45deg').addClass('bi-check-lg text-success');
                setTimeout(() => {
                    $icon.removeClass('bi-check-lg text-success').addClass('bi-link-45deg');
                }, 2000);
            });
        }
    });
});
