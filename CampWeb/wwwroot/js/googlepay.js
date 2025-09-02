// Google Pay – čistý JS + Blazor interop (bez změn backendu)
let paymentsClient = null;
let dotNetRef = null;
let currentPaymentData = null;

window.updateGooglePayContainer = function (html) {
    const el = document.getElementById('google-pay-button-container');
    if (el) el.innerHTML = html || '';
};

function loadGooglePayApi() {
    return new Promise(function (resolve, reject) {
        if (typeof google !== 'undefined' && google.payments && google.payments.api) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.async = true;
        script.src = 'https://pay.google.com/gp/p/js/pay.js';
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Nepodařilo se načíst Google Pay API'));
        document.head.appendChild(script);
    });
}

function getBaseRequest() {
    return { apiVersion: 2, apiVersionMinor: 0 };
}

function getTokenizationSpecification() {
    // ponecháváme 'example' – backend to může přijmout jako test
    return {
        type: 'PAYMENT_GATEWAY',
        parameters: {
            gateway: 'example',
            gatewayMerchantId: 'exampleGatewayMerchantId'
        }
    };
}

function getAllowedCardAuthMethods() { return ['PAN_ONLY', 'CRYPTOGRAM_3DS']; }
function getAllowedCardNetworks() { return ['VISA', 'MASTERCARD']; }

function getCardPaymentMethod() {
    return {
        type: 'CARD',
        parameters: {
            allowedAuthMethods: getAllowedCardAuthMethods(),
            allowedCardNetworks: getAllowedCardNetworks(),
            billingAddressRequired: false
        },
        tokenizationSpecification: getTokenizationSpecification()
    };
}

// Blazor volá: initializeGooglePay(paymentData, DotNetObjectReference)
window.initializeGooglePay = async function (paymentData, dotNetReference) {
    dotNetRef = dotNetReference;
    currentPaymentData = paymentData || {};

    try {
        await loadGooglePayApi();
    } catch (e) {
        console.error(e);
        updateGooglePayContainer(
            '<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Google Pay API není k dispozici</div>'
        );
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnGooglePayError', 'Google Pay API není k dispozici').catch(() => {});
        return;
    }

    try {
        paymentsClient = new google.payments.api.PaymentsClient({
            environment: (currentPaymentData.environment || 'TEST').toUpperCase()
        });

        const isReadyToPayReq = Object.assign({}, getBaseRequest(), {
            allowedPaymentMethods: [ getCardPaymentMethod() ]
        });

        const response = await paymentsClient.isReadyToPay(isReadyToPayReq);

        if (!response.result) {
            updateGooglePayContainer(
                '<div class="text-muted small text-center"><i class="fas fa-info-circle me-1"></i>Google Pay není podporován na tomto zařízení</div>'
            );
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnGooglePayError', 'Google Pay není podporován').catch(() => {});
            return;
        }

        // vykresli tlačítko
        const button = paymentsClient.createButton({
            onClick: onGooglePayButtonClicked
        });

        const container = document.getElementById('google-pay-button-container');
        if (!container) return;

        container.innerHTML = '';
        container.appendChild(button);

        if (dotNetRef) dotNetRef.invokeMethodAsync('OnGooglePayReady').catch(() => {});
    } catch (err) {
        console.error('Chyba při inicializaci Google Pay:', err);
        updateGooglePayContainer(
            '<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Chyba při načítání Google Pay</div>'
        );
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnGooglePayError', 'Inicializace selhala').catch(() => {});
    }
};

async function onGooglePayButtonClicked() {
    try {
        const price = String(currentPaymentData?.amount || '0.00');
        const currency = currentPaymentData?.currency || 'CZK';
        const description = currentPaymentData?.description || 'Platba';

        const paymentDataRequest = Object.assign({}, getBaseRequest(), {
            allowedPaymentMethods: [ getCardPaymentMethod() ],
            transactionInfo: {
                totalPriceStatus: 'FINAL',
                totalPrice: price,
                currencyCode: currency,
                countryCode: 'CZ'
            },
            merchantInfo: {
                merchantId: currentPaymentData?.merchantId || '01234567890123456789',
                merchantName: description
            }
        });

        const paymentData = await paymentsClient.loadPaymentData(paymentDataRequest);

        // token do Blazoru
        const token = paymentData.paymentMethodData?.tokenizationData?.token;
        if (token && dotNetRef) {
            await dotNetRef.invokeMethodAsync('ProcessGooglePayPayment', token);
        }
    } catch (err) {
        if (err && err.statusCode === 'CANCELED') {
            // uživatel zrušil – nehlásíme chybu
            return;
        }
        console.error('Google Pay error:', err);
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnGooglePayError', err.message || 'Chyba při zpracování').catch(() => {});
        }
    }
}
