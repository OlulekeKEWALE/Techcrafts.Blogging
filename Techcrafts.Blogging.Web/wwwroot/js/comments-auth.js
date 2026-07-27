$(document).ready(function() {
    $('#quick-email-auth-form').on('submit', function(e) {
        e.preventDefault();
        
        const $form = $(this);
        const $btn = $form.find('button[type="submit"]');
        const $spinner = $btn.find('.spinner-border');
        const $errorAlert = $('#quick-auth-error');
        
        // Disable button and show spinner
        $btn.prop('disabled', true);
        $spinner.removeClass('d-none');
        $errorAlert.addClass('d-none');
        
        $.ajax({
            url: '/account/quick-email-auth',
            type: 'POST',
            data: $form.serialize(),
            success: function(response) {
                if (response.success) {
                    // Instantly reload to show authenticated UI while preserving scroll via #comments in URL
                    window.location.reload();
                } else {
                    // Show error
                    $errorAlert.text(response.message || 'Authentication failed.').removeClass('d-none');
                    $btn.prop('disabled', false);
                    $spinner.addClass('d-none');
                }
            },
            error: function() {
                $errorAlert.text('A network error occurred. Please try again.').removeClass('d-none');
                $btn.prop('disabled', false);
                $spinner.addClass('d-none');
            }
        });
    });
});
