// Apple Pay (only JS + Blazor interop; žádný serverový controller není nutný)
let applePaySession = null;
let applePayDotNetRef = null;
let applePaymentData = null;

window.updateApplePayContainer = function (html) {
    const el = document.getElementById('apple-pay-button-container');
    if (el) el.innerHTML = html || '';
};

// Blazor volá: initializeApplePay(paymentData, DotNetObjectReference)
window.initializeApplePay = function (paymentData, dotNetReference) {
    applePayDotNetRef = dotNetReference;
    applePaymentData = paymentData || {};

    if (!window.ApplePaySession) {
        console.warn('Apple Pay není v tomto prohlížeči k dispozici.');
        updateApplePayContainer(
            '<div class="text-muted small text-center"><i class="fas fa-exclamation-circle me-1"></i>Apple Pay není k dispozici</div>'
        );
        if (applePayDotNetRef) {
            applePayDotNetRef.invokeMethodAsync('OnApplePayError', 'Apple Pay není k dispozici').catch(() => {});
        }
        return;
    }

    // Zobraz tlačítko jen pokud zařízení podporuje Apple Pay
    if (ApplePaySession.canMakePayments()) {
        addApplePayButton();
    } else {
        console.warn('Zařízení Apple Pay nepodporuje.');
        updateApplePayContainer(
            '<div class="text-muted small text-center"><i class="fas fa-info-circle me-1"></i>Apple Pay není podporován</div>'
        );
        if (applePayDotNetRef) {
            applePayDotNetRef.invokeMethodAsync('OnApplePayError', 'Apple Pay není podporován').catch(() => {});
        }
    }
};

function addApplePayButton() {
    const container = document.getElementById('apple-pay-button-container');
    if (!container) return;

    container.innerHTML = '';

    const btn = document.createElement('div');
    btn.className = 'apple-pay-button apple-pay-button-black';
    btn.addEventListener('click', onApplePayButtonClicked);

    container.appendChild(btn);

    if (applePayDotNetRef) {
        applePayDotNetRef.invokeMethodAsync('OnApplePayReady').catch(() => {});
    }
}

function onApplePayButtonClicked() {
    if (!applePaymentData) return;

    const merchantId = applePaymentData.merchantId || 'merchant.invalid';
    const amount = String(applePaymentData.amount || '0.00');
    const currency = applePaymentData.currency || 'CZK';
    const description = applePaymentData.description || 'Platba';

    const request = {
        countryCode: 'CZ',
        currencyCode: currency,
        supportedNetworks: ['visa', 'masterCard'],
        merchantCapabilities: ['supports3DS'],
        merchantIdentifier: merchantId,
        total: {
            label: description,
            amount: amount
        },
        lineItems: [
            { label: description, amount: amount, type: 'final' }
        ],
        requiredBillingContactFields: [],
        requiredShippingContactFields: []
    };

    const session = new ApplePaySession(3, request);
    applePaySession = session;

    // Merchant validation — **žádný controller nepotřebujeme**: voláme JSInvokable v Razor komponentě
    session.onvalidatemerchant = function (event) {
        // 1) preferujeme Blazor metodu (ValidateApplePayMerchant)
        if (applePayDotNetRef && typeof applePayDotNetRef.invokeMethodAsync === 'function') {
            applePayDotNetRef.invokeMethodAsync('ValidateApplePayMerchant', event.validationURL)
                .then(function (merchantSessionJson) {
                    let merchantSession = merchantSessionJson;
                    // pokud Blazor vrátil string, zkusíme parse
                    if (typeof merchantSessionJson === 'string') {
                        try { merchantSession = JSON.parse(merchantSessionJson); } catch (e) {}
                    }
                    if (!merchantSession || typeof merchantSession !== 'object') {
                        throw new Error('Neplatná merchant session');
                    }
                    session.completeMerchantValidation(merchantSession);
                })
                .catch(function (err) {
                    console.error('Merchant validation selhala:', err);
                    session.abort();
                    if (applePayDotNetRef) {
                        applePayDotNetRef.invokeMethodAsync('OnApplePayError', 'Merchant validation failed').catch(() => {});
                    }
                });
            return;
        }

        // 2) fallback – pokud by existovalo API (nevyžadováno)
        fetch('/api/applepay/validate-merchant', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ validationUrl: event.validationURL })
        })
            .then(r => r.ok ? r.json() : Promise.reject(new Error('Merchant validation failed')))
            .then(ms => session.completeMerchantValidation(ms))
            .catch(err => {
                console.error('Merchant validation selhala:', err);
                session.abort();
                if (applePayDotNetRef) {
                    applePayDotNetRef.invokeMethodAsync('OnApplePayError', 'Merchant validation failed').catch(() => {});
                }
            });
    };

    // Uživatel vybral kartu – můžeme aktualizovat total (volitelné)
    session.onpaymentmethodselected = function () {
        session.completePaymentMethodSelection({
            label: description,
            amount: amount
        });
    };

    // Uživatel autorizoval platbu – token odešleme do Blazoru
    session.onpaymentauthorized = function (ev) {
        try {
            const paymentToken = JSON.stringify(ev.payment.token);
            if (applePayDotNetRef) {
                applePayDotNetRef.invokeMethodAsync('ProcessApplePayPayment', paymentToken)
                    .then(function () {
                        session.completePayment(ApplePaySession.STATUS_SUCCESS);
                    })
                    .catch(function (error) {
                        console.error('Chyba při zpracování platby:', error);
                        session.completePayment(ApplePaySession.STATUS_FAILURE);
                    });
            } else {
                session.completePayment(ApplePaySession.STATUS_FAILURE);
            }
        } catch (e) {
            console.error('Apple Pay token error:', e);
            session.completePayment(ApplePaySession.STATUS_FAILURE);
        }
    };

    session.oncancel = function () {
        console.log('Apple Pay zrušeno uživatelem.');
    };

    session.begin();
}

// Minimal styl tlačítka (nativní -apple-pay-button s fallbackem)
(function ensureAppleStyles() {
    const style = document.createElement('style');
    style.textContent = `
.apple-pay-button {
    display: inline-block;
    -webkit-appearance: -apple-pay-button;
    -apple-pay-button-type: buy;
    -apple-pay-button-style: black;
    width: 200px;
    height: 44px;
    border-radius: 8px;
    cursor: pointer;
}
@supports not (-webkit-appearance: -apple-pay-button) {
    .apple-pay-button { display: none; }
}`;
    document.head.appendChild(style);
})();
