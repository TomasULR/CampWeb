// Google Pay API Configuration
const baseRequest = {
    apiVersion: 2,
    apiVersionMinor: 0
};

const allowedCardNetworks = ["MASTERCARD", "VISA"];
const allowedCardAuthMethods = ["PAN_ONLY", "CRYPTOGRAM_3DS"];

const tokenizationSpecification = {
    type: 'PAYMENT_GATEWAY',
    parameters: {
        'gateway': 'example',
        'gatewayMerchantId': 'exampleGatewayMerchantId'
    }
};

const baseCardPaymentMethod = {
    type: 'CARD',
    parameters: {
        allowedAuthMethods: allowedCardAuthMethods,
        allowedCardNetworks: allowedCardNetworks
    }
};

const cardPaymentMethod = Object.assign(
    {},
    baseCardPaymentMethod,
    {
        tokenizationSpecification: tokenizationSpecification
    }
);

let paymentsClient = null;
let dotNetRef = null;
let currentPaymentData = null;

function getGoogleIsReadyToPayRequest() {
    return Object.assign(
        {},
        baseRequest,
        {
            allowedPaymentMethods: [baseCardPaymentMethod]
        }
    );
}

function getGooglePaymentDataRequest(paymentData) {
    const paymentDataRequest = Object.assign({}, baseRequest);
    paymentDataRequest.allowedPaymentMethods = [cardPaymentMethod];
    paymentDataRequest.transactionInfo = {
        totalPriceStatus: 'FINAL',
        totalPriceLabel: 'Celkem',
        totalPrice: paymentData.amount,
        currencyCode: paymentData.currency || 'CZK',
        countryCode: 'CZ'
    };
    paymentDataRequest.merchantInfo = {
        merchantName: 'Letní Tábory Plzeň'
        // merchantId is only needed in production
    };

    return paymentDataRequest;
}

function getGooglePaymentsClient() {
    if (paymentsClient === null) {
        paymentsClient = new google.payments.api.PaymentsClient({
            environment: 'TEST' // Change to 'PRODUCTION' when going live
        });
    }
    return paymentsClient;
}

function onPaymentAuthorized(paymentData) {
    return new Promise(function(resolve, reject) {
        console.log('Payment authorized:', paymentData);
        processPayment(paymentData)
            .then(function() {
                resolve({ transactionState: 'SUCCESS' });
            })
            .catch(function(error) {
                console.error('Payment processing failed:', error);
                resolve({
                    transactionState: 'ERROR',
                    error: {
                        intent: 'PAYMENT_AUTHORIZATION',
                        message: 'Platba se nezdařila. Zkuste to prosím znovu.',
                        reason: 'PAYMENT_DECLINED'
                    }
                });
            });
    });
}

function processPayment(paymentData) {
    return new Promise(function(resolve, reject) {
        try {
            const paymentToken = paymentData.paymentMethodData.tokenizationData.token;
            console.log('Processing payment with token...');

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('ProcessGooglePayPayment', paymentToken)
                    .then(function() {
                        console.log('Payment processed successfully');
                        resolve();
                    })
                    .catch(function(error) {
                        console.error('Payment processing error:', error);
                        reject(error);
                    });
            } else {
                console.error('DotNet reference not available');
                reject('DotNet reference not available');
            }
        } catch (error) {
            console.error('Error in processPayment:', error);
            reject(error);
        }
    });
}

function onGooglePaymentButtonClicked() {
    console.log('Google Pay button clicked');

    if (!currentPaymentData) {
        console.error('Payment data not available');
        return;
    }

    const paymentDataRequest = getGooglePaymentDataRequest(currentPaymentData);
    const paymentsClient = getGooglePaymentsClient();

    paymentsClient.loadPaymentData(paymentDataRequest)
        .then(function(paymentData) {
            console.log('Payment data loaded successfully');
            return onPaymentAuthorized(paymentData);
        })
        .then(function(result) {
            if (result.transactionState === 'SUCCESS') {
                console.log('Transaction successful');
            } else {
                console.error('Transaction failed:', result.error);
            }
        })
        .catch(function(err) {
            console.error('Load payment data error:', err);
            if (dotNetRef) {
                let errorMessage = 'Payment failed';
                if (err.statusCode === 'CANCELED') {
                    errorMessage = 'Payment canceled';
                } else if (err.statusMessage) {
                    errorMessage = err.statusMessage;
                }
                dotNetRef.invokeMethodAsync('OnGooglePayError', errorMessage)
                    .catch(function(error) {
                        console.error('Error calling OnGooglePayError:', error);
                    });
            }
        });
}

function addGooglePayButton() {
    try {
        const paymentsClient = getGooglePaymentsClient();
        const button = paymentsClient.createButton({
            onClick: onGooglePaymentButtonClicked,
            buttonColor: 'black',
            buttonType: 'pay',
            buttonSizeMode: 'fill'
        });

        const container = document.getElementById('google-pay-button-container');
        if (container) {
            container.innerHTML = '';
            container.appendChild(button);

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnGooglePayReady')
                    .catch(function(error) {
                        console.error('Error calling OnGooglePayReady:', error);
                    });
            }

            console.log('Google Pay button added successfully');
        } else {
            console.error('Google Pay button container not found');
        }
    } catch (error) {
        console.error('Error creating Google Pay button:', error);
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnGooglePayError', error.message || 'Button creation failed')
                .catch(function(err) {
                    console.error('Error calling OnGooglePayError:', err);
                });
        }
    }
}

// Utility function
window.updateGooglePayContainer = function(html) {
    const container = document.getElementById('google-pay-button-container');
    if (container) {
        container.innerHTML = html;
    }
};

// Main initialization function
window.initializeGooglePay = function(paymentData, dotNetReference) {
    console.log('Initializing Google Pay with data:', paymentData);

    // Store references
    dotNetRef = dotNetReference;
    currentPaymentData = paymentData;

    // Check if Google Pay API is available
    if (typeof google === 'undefined' || !google.payments || !google.payments.api) {
        console.error('Google Pay API is not loaded');
        window.updateGooglePayContainer('<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Google Pay API není k dispozici</div>');
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnGooglePayError', 'Google Pay API není k dispozici')
                .catch(function(error) {
                    console.error('Error calling OnGooglePayError:', error);
                });
        }
        return;
    }

    try {
        const paymentsClient = getGooglePaymentsClient();
        paymentsClient.isReadyToPay(getGoogleIsReadyToPayRequest())
            .then(function(response) {
                console.log('Google Pay readiness response:', response);
                if (response.result) {
                    addGooglePayButton();
                } else {
                    console.log('Google Pay is not available');
                    window.updateGooglePayContainer('<div class="text-muted small text-center"><i class="fas fa-info-circle me-1"></i>Google Pay není podporován na tomto zařízení</div>');
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('OnGooglePayError', 'Google Pay není podporován')
                            .catch(function(error) {
                                console.error('Error calling OnGooglePayError:', error);
                            });
                    }
                }
            })
            .catch(function(err) {
                console.error('Error determining Google Pay availability:', err);
                window.updateGooglePayContainer('<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Chyba při načítání Google Pay</div>');
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnGooglePayError', err.message || 'Google Pay initialization failed')
                        .catch(function(error) {
                            console.error('Error calling OnGooglePayError:', error);
                        });
                }
            });
    } catch (error) {
        console.error('Error in initializeGooglePay:', error);
        window.updateGooglePayContainer('<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Chyba inicializace</div>');
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnGooglePayError', error.message || 'Initialization error')
                .catch(function(err) {
                    console.error('Error calling OnGooglePayError:', err);
                });
        }
    }
};