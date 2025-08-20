// Apple Pay Configuration
let applePaySession = null;
let applePayDotNetRef = null;
let applePaymentData = null;

// Check if Apple Pay is available
window.checkApplePayAvailability = function() {
    if (window.ApplePaySession) {
        const merchantIdentifier = 'merchant.com.letnitabory'; // Replace with your merchant ID
        const promise = ApplePaySession.canMakePaymentsWithActiveCard(merchantIdentifier);
        return promise.then(function(canMakePayments) {
            return canMakePayments;
        });
    }
    return Promise.resolve(false);
};

// Initialize Apple Pay
window.initializeApplePay = function(paymentData, dotNetReference) {
    console.log('Initializing Apple Pay with data:', paymentData);

    applePayDotNetRef = dotNetReference;
    applePaymentData = paymentData;

    // Check if Apple Pay is available
    if (!window.ApplePaySession) {
        console.log('Apple Pay is not available in this browser');
        updateApplePayContainer('<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Apple Pay není k dispozici</div>');
        return;
    }

    // Check if the device supports Apple Pay
    if (ApplePaySession.canMakePayments()) {
        addApplePayButton();
    } else {
        console.log('Apple Pay is not supported on this device');
        updateApplePayContainer('<div class="text-muted small text-center"><i class="fas fa-info-circle me-1"></i>Apple Pay není podporován</div>');
    }
};

function addApplePayButton() {
    const container = document.getElementById('apple-pay-button-container');
    if (container) {
        container.innerHTML = '';

        // Create Apple Pay button
        const button = document.createElement('div');
        button.className = 'apple-pay-button apple-pay-button-black';
        button.onclick = onApplePayButtonClicked;

        container.appendChild(button);
        console.log('Apple Pay button added successfully');

        if (applePayDotNetRef) {
            applePayDotNetRef.invokeMethodAsync('OnApplePayReady')
                .catch(function(error) {
                    console.error('Error calling OnApplePayReady:', error);
                });
        }
    }
}

function onApplePayButtonClicked() {
    console.log('Apple Pay button clicked');

    if (!applePaymentData) {
        console.error('Payment data not available');
        return;
    }

    const request = {
        countryCode: 'CZ',
        currencyCode: applePaymentData.currency || 'CZK',
        supportedNetworks: ['visa', 'masterCard', 'amex'],
        merchantCapabilities: ['supports3DS'],
        total: {
            label: applePaymentData.description || 'Letní Tábory Plzeň',
            amount: applePaymentData.amount
        }
    };

    // Create Apple Pay session (version 3)
    const session = new ApplePaySession(3, request);

    // Merchant validation
    session.onvalidatemerchant = function(event) {
        console.log('Validating merchant:', event.validationURL);

        // Call your backend API to validate with Apple
        fetch('/api/applepay/validate-merchant', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                validationUrl: event.validationURL
            })
        })
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Merchant validation failed');
                }
                return response.json();
            })
            .then(function(merchantSession) {
                console.log('Merchant session received:', merchantSession);
                session.completeMerchantValidation(merchantSession);
            })
            .catch(function(error) {
                console.error('Merchant validation failed:', error);
                session.abort();
                if (applePayDotNetRef) {
                    applePayDotNetRef.invokeMethodAsync('OnApplePayError', 'Merchant validation failed')
                        .catch(function(err) {
                            console.error('Error calling OnApplePayError:', err);
                        });
                }
            });
    };

    // Payment method selected
    session.onpaymentmethodselected = function(event) {
        console.log('Payment method selected:', event.paymentMethod);
        const total = {
            label: applePaymentData.description || 'Letní Tábory Plzeň',
            amount: applePaymentData.amount
        };
        session.completePaymentMethodSelection(total);
    };

    // Payment authorized
    session.onpaymentauthorized = function(event) {
        console.log('Payment authorized:', event.payment);

        if (applePayDotNetRef) {
            // Send payment token to server
            const paymentToken = JSON.stringify(event.payment.token);

            applePayDotNetRef.invokeMethodAsync('ProcessApplePayPayment', paymentToken)
                .then(function() {
                    // Payment successful
                    session.completePayment(ApplePaySession.STATUS_SUCCESS);
                })
                .catch(function(error) {
                    // Payment failed
                    console.error('Payment processing failed:', error);
                    session.completePayment(ApplePaySession.STATUS_FAILURE);
                });
        } else {
            session.completePayment(ApplePaySession.STATUS_FAILURE);
        }
    };

    // Handle cancel
    session.oncancel = function(event) {
        console.log('Apple Pay cancelled');
        if (applePayDotNetRef) {
            applePayDotNetRef.invokeMethodAsync('OnApplePayError', 'Payment cancelled')
                .catch(function(error) {
                    console.error('Error calling OnApplePayError:', error);
                });
        }
    };

    // Begin session
    session.begin();
    applePaySession = session;
}

// Utility function to update container
function updateApplePayContainer(html) {
    const container = document.getElementById('apple-pay-button-container');
    if (container) {
        container.innerHTML = html;
    }
}

// Add Apple Pay button styles
const style = document.createElement('style');
style.textContent = `
    .apple-pay-button {
        display: inline-block;
        -webkit-appearance: -apple-pay-button;
        -apple-pay-button-type: pay;
        width: 100%;
        height: 50px;
        cursor: pointer;
    }
    
    .apple-pay-button-black {
        -apple-pay-button-style: black;
    }
    
    .apple-pay-button-white {
        -apple-pay-button-style: white;
    }
    
    .apple-pay-button-white-outline {
        -apple-pay-button-style: white-outline;
    }
    
    @supports not (-webkit-appearance: -apple-pay-button) {
        .apple-pay-button {
            display: none;
        }
    }
`;
document.head.appendChild(style);