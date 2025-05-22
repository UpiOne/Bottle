mergeInto(LibraryManager.library, {

    // Checks if the basic Telegram WebApp structure exists.
    // Returns true (1) or false (0).
    IsTelegramEnvironment: function() {
        try {
            // Check if running in a browser environment and if Telegram WebApp object exists
            return typeof window !== 'undefined' &&
                   typeof window.Telegram !== 'undefined' &&
                   typeof window.Telegram.WebApp !== 'undefined';
        } catch (e) {
            console.error("Error checking IsTelegramEnvironment:", e);
            return false;
        }
    },

   IsTelegramWebAppAvailable: function() {
       try {
           return typeof window !== 'undefined' &&
                  typeof window.Telegram !== 'undefined' &&
                  typeof window.Telegram.WebApp !== 'undefined' &&
                  typeof window.Telegram.WebApp.initData !== 'undefined' && // ОБЯЗАТЕЛЬНО
                  typeof window.Telegram.WebApp.initData === 'string' &&   // ОБЯЗАТЕЛЬНО
                  window.Telegram.WebApp.initData.length > 0 &&         // ОБЯЗАТЕЛЬНО
                  typeof window.Telegram.WebApp.ready === 'function';
       } catch (e) {
           // console.error("Error checking IsTelegramWebAppAvailable:", e); // Можно закомментировать, чтобы не спамило в консоль при каждой проверке
           return false;
       }
   },

    GetTelegramWebAppInitData: function() {
        try {
            if (typeof window !== 'undefined' && window.Telegram && window.Telegram.WebApp && window.Telegram.WebApp.initData) {
                const initData = window.Telegram.WebApp.initData;
                if (initData && typeof initData === 'string' && initData.length > 0) {
                    const bufferSize = lengthBytesUTF8(initData) + 1;
                    const buffer = _malloc(bufferSize);
                    if (buffer === 0) {
                        console.error("Failed to allocate memory for initData buffer.");
                        return 0; // Return null pointer if malloc failed
                    }
                    stringToUTF8(initData, buffer, bufferSize);
                    return buffer;
                } else {
                    console.warn("Telegram.WebApp.initData is empty, null, or not a string.");
                    return 0; // Return null pointer if data is empty/invalid
                }
            } else {
                console.warn("Telegram WebApp or initData not available when GetTelegramWebAppInitData was called.");
                return 0; // Return null pointer if API is not available
            }
        } catch (e) {
            console.error("Error in GetTelegramWebAppInitData:", e);
            return 0; // Return null pointer on error
        }
    },

    // Добавляем новую функцию для получения модифицированного initData с добавленным реферальным кодом
   // В вашем .jslib файле
   GetTelegramWebAppInitDataWithReferal: function() {
       try {
           // Убедимся, что API действительно готово и initData ЕСТЬ
           if (typeof window !== 'undefined' &&
               window.Telegram &&
               window.Telegram.WebApp &&
               window.Telegram.WebApp.initData && // Ключевая проверка!
               typeof window.Telegram.WebApp.initData === 'string' && // Убедимся, что это строка
               window.Telegram.WebApp.initData.length > 0) { // Убедимся, что не пустая
   
               let initData = window.Telegram.WebApp.initData; // Берем уже готовую строку
               let originalInitDataForLog = initData; // Сохраняем для лога
               console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Получена оригинальная initData: ' + originalInitDataForLog);
   
               let referalCode = '';
   
               if (window.Telegram.WebApp.startParam && typeof window.Telegram.WebApp.startParam === 'string' && window.Telegram.WebApp.startParam.length > 0) {
                   referalCode = window.Telegram.WebApp.startParam;
                   console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Найден код в startParam: ' + referalCode);
               } else if (window.Telegram.WebApp.initDataUnsafe && window.Telegram.WebApp.initDataUnsafe.start_param && typeof window.Telegram.WebApp.initDataUnsafe.start_param === 'string' && window.Telegram.WebApp.initDataUnsafe.start_param.length > 0) {
                   referalCode = window.Telegram.WebApp.initDataUnsafe.start_param;
                   console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Найден код в initDataUnsafe.start_param: ' + referalCode);
               } else if (localStorage.getItem('bottle_game_referal_code')) { // localStorage - наименее приоритетный
                   referalCode = localStorage.getItem('bottle_game_referal_code');
                   console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Найден код в localStorage: ' + referalCode);
               }
   
               if (referalCode) {
                   // Проверяем, не содержится ли уже start_param в initData
                   // Это важно, чтобы не дублировать и не ломать, если Telegram сам его добавил
                   const parsedParams = new URLSearchParams(initData);
                   if (!parsedParams.has('start_param')) {
                       console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Добавляем start_param ('+ referalCode +') в initData.');
                       // Аккуратно добавляем, чтобы не сломать существующие query-параметры
                       if (initData.length > 0 && !initData.endsWith('&') && !initData.endsWith('?')) {
                           initData += '&';
                       } else if (initData.length === 0) {
                           // Если initData была пустой (маловероятно, но на всякий случай)
                           // не нужно начинать с '&'
                       }
                       initData += 'start_param=' + encodeURIComponent(referalCode);
                       console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Модифицированная initData: ' + initData);
                   } else {
                       console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: initData уже содержит start_param. Текущее значение: ' + parsedParams.get('start_param') + '. Новый найденный код: ' + referalCode + '. Не перезаписываем.');
                   }
               } else {
                   console.log('[REFERAL DEBUG JS] GetTelegramWebAppInitDataWithReferal: Реферальный код для добавления в initData не найден.');
               }
   
               // Возвращаем initData (модифицированную или оригинальную)
               // Повторная проверка на случай, если initData стала пустой после манипуляций (не должно быть)
               if (initData && typeof initData === 'string' && initData.length > 0) {
                   const bufferSize = lengthBytesUTF8(initData) + 1;
                   const buffer = _malloc(bufferSize);
                   if (buffer === 0) {
                       console.error("Failed to allocate memory for initData buffer in GetTelegramWebAppInitDataWithReferal.");
                       return 0;
                   }
                   stringToUTF8(initData, buffer, bufferSize);
                   return buffer;
               } else {
                   console.warn("GetTelegramWebAppInitDataWithReferal: initData is empty, null, or not a string after processing. Оригинальная была: " + originalInitDataForLog);
                   return 0;
               }
   
           } else {
               console.warn("Telegram WebApp or its initData not available/valid when GetTelegramWebAppInitDataWithReferal was called.");
               // Логируем состояние WebApp для диагностики
               if (typeof window.Telegram === 'undefined') console.warn("- window.Telegram is undefined");
               else if (typeof window.Telegram.WebApp === 'undefined') console.warn("- window.Telegram.WebApp is undefined");
               else if (typeof window.Telegram.WebApp.initData === 'undefined') console.warn("- window.Telegram.WebApp.initData is undefined");
               else if (typeof window.Telegram.WebApp.initData !== 'string') console.warn("- window.Telegram.WebApp.initData is not a string: " + typeof window.Telegram.WebApp.initData);
               else if (window.Telegram.WebApp.initData.length === 0) console.warn("- window.Telegram.WebApp.initData is an empty string");
   
               return 0; // Return null pointer if API is not available or initData invalid
           }
       } catch (e) {
           console.error("Error in GetTelegramWebAppInitDataWithReferal:", e);
           return 0; // Return null pointer on error
       }
   },

    // Копирует текст в буфер обмена
    // Возвращает true в случае успеха, false при ошибке
    CopyToClipboard: function(textPtr) {
        try {
            // Преобразуем указатель на строку в JavaScript строку
            const text = UTF8ToString(textPtr);
            console.log("CopyToClipboard: Пытаемся скопировать текст: " + text);

            // Используем document.execCommand (самый надежный в WebGL и Telegram контексте)
            console.log("CopyToClipboard: Пробуем метод execCommand для прямого копирования");
            const tempInput = document.createElement("textarea");
            tempInput.value = text;

            // Критично для работы в мобильных браузерах
            tempInput.style.position = "fixed";
            tempInput.style.left = "0";
            tempInput.style.top = "0";
            tempInput.style.opacity = "0";
            tempInput.style.pointerEvents = "none";
            document.body.appendChild(tempInput);

            // Фокус и выделение
            tempInput.focus();
            tempInput.select();
            tempInput.setSelectionRange(0, 99999); // Для мобильных устройств

            // Пытаемся скопировать
            var success = false;
            try {
                success = document.execCommand('copy');
                if (success) {
                    console.log("CopyToClipboard: Текст успешно скопирован через execCommand");
                } else {
                    console.warn("CopyToClipboard: execCommand вернул false");
                }
            } catch (err) {
                console.error("CopyToClipboard: Ошибка при вызове execCommand:", err);
            }

            // Удаляем временный элемент
            document.body.removeChild(tempInput);

            // В случае успеха
            if (success) {
                return true;
            }

            // Если не получилось, пробуем navigator.clipboard
            if (navigator && navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
                console.log("CopyToClipboard: Пробуем метод navigator.clipboard как запасной вариант");
                navigator.clipboard.writeText(text).catch(function(err) {
                    console.error("Ошибка при копировании через navigator.clipboard:", err);
                });
                // Возвращаем true даже если будет ошибка, пользователю будет показан текст для ручного копирования
                return true;
            }

            // Если ничего не сработало, выводим текст для ручного копирования
            console.log("CopyToClipboard: Показываем текст для ручного копирования");
            window.alert("Скопируйте эту ссылку вручную:\n\n" + text);

            // Всегда возвращаем true, чтобы в Unity не показывалась ошибка
            return true;
        } catch (e) {
            console.error("CopyToClipboard: Критическая ошибка:", e);

            // Показываем текст даже при ошибке
            try {
                window.alert("Скопируйте эту ссылку вручную:\n\n" + UTF8ToString(textPtr));
            } catch(err) {
                // Игнорируем ошибки здесь
            }

            // Всегда возвращаем true
            return true;
        }
    },

    // Новая функция для поиска реферального кода в JS и отправки его в Unity
    FindAndSendReferalCodeToUnity: function(unityGameObjectNamePtr) {
        var referalCode = ''; // Объявляем здесь, чтобы была видна везде
        try {
            var unityGameObjectName = UTF8ToString(unityGameObjectNamePtr);
            // --- НАЧАЛО ИЗМЕНЕНИЙ ---
            console.log('[REFERAL DEBUG JS] FindAndSendReferalCodeToUnity ЗАПУЩЕНА для объекта: ' + unityGameObjectName);

            var url = window.location.href;
            console.log('[REFERAL DEBUG JS] Текущий URL: ' + url);

            // 1. Проверяем сначала WebApp API - самый надежный источник
            if (typeof window !== 'undefined' && window.Telegram && window.Telegram.WebApp) {
                console.log('[REFERAL DEBUG JS] Telegram WebApp API доступен.');
                // Логируем важные параметры WebApp
                console.log('[REFERAL DEBUG JS] WebApp.startParam:', window.Telegram.WebApp.startParam);
                console.log('[REFERAL DEBUG JS] WebApp.initDataUnsafe:', window.Telegram.WebApp.initDataUnsafe);
                // console.log('[REFERAL DEBUG JS] WebApp.initData:', window.Telegram.WebApp.initData); // Можно раскомментировать для полной отладки initData

                if (window.Telegram.WebApp.startParam && typeof window.Telegram.WebApp.startParam === 'string' && window.Telegram.WebApp.startParam.length > 0) {
                    referalCode = window.Telegram.WebApp.startParam;
                    console.log('[REFERAL DEBUG JS] Код найден в WebApp.startParam: ' + referalCode);
                }
                // Дополнительно проверим initDataUnsafe, если startParam пуст
                else if (window.Telegram.WebApp.initDataUnsafe && window.Telegram.WebApp.initDataUnsafe.start_param && typeof window.Telegram.WebApp.initDataUnsafe.start_param === 'string' && window.Telegram.WebApp.initDataUnsafe.start_param.length > 0) {
                     referalCode = window.Telegram.WebApp.initDataUnsafe.start_param;
                     console.log('[REFERAL DEBUG JS] Код найден в initDataUnsafe.start_param: ' + referalCode);
                }
                 // Можно добавить и проверку полной initData (как было раньше), если startParam и initDataUnsafe пусты
                 else if (window.Telegram.WebApp.initData && window.Telegram.WebApp.initData.indexOf('start_param=') > -1) {
                    var parts = window.Telegram.WebApp.initData.split('&');
                    for (var i = 0; i < parts.length; i++) {
                        if (parts[i].indexOf('start_param=') === 0) {
                            try {
                                referalCode = decodeURIComponent(parts[i].substring('start_param='.length));
                                console.log('[REFERAL DEBUG JS] Код найден в полной initData (декодирован): ' + referalCode);
                            } catch(e) {
                                console.warn('[REFERAL DEBUG JS] Ошибка декодирования start_param из initData: ' + e.message);
                                referalCode = parts[i].substring('start_param='.length);
                                console.log('[REFERAL DEBUG JS] Код найден в полной initData (НЕ декодирован из-за ошибки): ' + referalCode);
                            }
                            break;
                        }
                    }
                 } else {
                    console.log('[REFERAL DEBUG JS] WebApp API доступен, но startParam и start_param в initDataUnsafe/initData не найдены или пусты.');
                 }
            } else {
                 console.log('[REFERAL DEBUG JS] Telegram WebApp API НЕДОСТУПЕН!');
            }

            // 2. Если не нашли в WebApp, пробуем искать в URL (менее надежно, особенно в iframe)
            if (!referalCode) {
                console.log('[REFERAL DEBUG JS] Код НЕ найден в WebApp API, проверяем URL...');
                if (url.includes('?startparam=')) { // Явная проверка параметра как у вас в ссылке
                    referalCode = url.split('?startparam=')[1].split('&')[0].split('#')[0]; // Убираем хвосты
                    console.log('[REFERAL DEBUG JS] Найден параметр ?startparam= в URL: ' + referalCode);
                } else if (url.includes('?startapp=')) { // Стандартный параметр для WebApp из URL
                    referalCode = url.split('?startapp=')[1].split('&')[0].split('#')[0];
                    console.log('[REFERAL DEBUG JS] Найден параметр ?startapp= в URL: ' + referalCode);
                } else if (url.includes('?start=')) { // Стандартный параметр для ботов
                    referalCode = url.split('?start=')[1].split('&')[0].split('#')[0];
                    console.log('[REFERAL DEBUG JS] Найден параметр ?start= в URL: ' + referalCode);
                }
                // Проверку на 'ref_' можно оставить как запасную, но она менее точная
                // else if (url.includes('ref_')) {
                //     var startIndex = url.indexOf('ref_');
                //     referalCode = url.substring(startIndex);
                //     // Обрезаем лишнее после кода
                //     if (referalCode.includes('&')) referalCode = referalCode.split('&')[0];
                //     if (referalCode.includes('?')) referalCode = referalCode.split('?')[0]; // На всякий случай
                //     if (referalCode.includes('#')) referalCode = referalCode.split('#')[0];
                //     console.log('[REFERAL DEBUG JS] Найден паттерн ref_ в URL: ' + referalCode);
                // }
                 else {
                    console.log('[REFERAL DEBUG JS] В URL параметры startparam/startapp/start не найдены.');
                 }
            }
             // --- КОНЕЦ ИЗМЕНЕНИЙ В ЛОГИКЕ ПОИСКА ---

            // 3. Декодирование, сохранение и отправка в Unity, если код найден
            if (referalCode) {
                // Пытаемся декодировать компонент URI, если он закодирован
                try {
                    var decodedCode = decodeURIComponent(referalCode);
                    if (decodedCode !== referalCode) {
                        console.log('[REFERAL DEBUG JS] Код был декодирован: ' + decodedCode);
                        referalCode = decodedCode;
                    } else {
                         console.log('[REFERAL DEBUG JS] Декодирование не изменило код (вероятно, он не был закодирован).');
                    }
                } catch(decodeErr) {
                    console.warn('[REFERAL DEBUG JS] Ошибка при попытке декодирования URI компонента, используем код как есть:', decodeErr);
                }

                console.log('[REFERAL DEBUG JS] ИТОГ: Реферальный код найден и обработан: "' + referalCode + '"');

                // Сохраняем код в localStorage, чтобы Unity мог его прочитать при необходимости
                try {
                    localStorage.setItem('bottle_game_referal_code', referalCode);
                    console.log('[REFERAL DEBUG JS] Реферальный код сохранен в localStorage.');
                } catch(storageErr) {
                    console.error('[REFERAL DEBUG JS] Ошибка сохранения в localStorage:', storageErr);
                }

                // Отправляем код в Unity через SendMessage
                 try {
                     // Пытаемся найти инстанс Unity (может называться по-разному в зависимости от версии Unity и шаблона)
                     var unityInstance = window.unityInstance || window.gameInstance; // Пробуем оба общих имени

                     if (unityInstance && typeof unityInstance.SendMessage === 'function') {
                         console.log('[REFERAL DEBUG JS] Отправка кода в Unity через SendMessage на объект "' + unityGameObjectName + '" методом "ProcessReferalCodeFromWebApp"');
                         unityInstance.SendMessage(unityGameObjectName, 'ProcessReferalCodeFromWebApp', referalCode);
                         return 1; // Успешно нашли и попытались отправить
                     } else {
                         console.warn('[REFERAL DEBUG JS] Не удалось найти unityInstance или gameInstance для вызова SendMessage. Код сохранен в localStorage.');
                         // Если SendMessage не найден, Unity должен будет сам прочитать из localStorage позже
                         return 1; // Считаем успехом, так как код найден и сохранен
                     }
                 } catch (sendMessageError) {
                     console.error('[REFERAL DEBUG JS] Ошибка при вызове SendMessage:', sendMessageError);
                      // Все равно считаем успехом, так как код найден и сохранен в localStorage
                      return 1;
                 }

            } else {
                console.log('[REFERAL DEBUG JS] ИТОГ: Реферальный код НЕ найден ни в WebApp API, ни в URL.');
                return 0; // Код не найден
            }

        } catch (e) {
            console.error('[REFERAL DEBUG JS] Критическая ошибка в FindAndSendReferalCodeToUnity:', e);
            // В случае ошибки, все равно возвращаем 0, чтобы не сломать C# код, ожидающий int
            return 0; // Возвращаем 0 при ошибке
        }
    },

    // Добавляем новую функцию для чтения реферального кода из localStorage
    GetSavedReferalCode: function() {
        try {
            var code = localStorage.getItem('bottle_game_referal_code') || '';
            if (code) {
                console.log('[REFERAL DEBUG JS] Чтение сохраненного кода из localStorage: ' + code);
                var bufferSize = lengthBytesUTF8(code) + 1;
                var buffer = _malloc(bufferSize);
                if (buffer === 0) {
                    console.error("[REFERAL DEBUG JS] Не удалось выделить память для GetSavedReferalCode");
                    return 0;
                }
                stringToUTF8(code, buffer, bufferSize);
                return buffer; // Возвращаем указатель на строку в памяти Emscripten
            } else {
                console.log('[REFERAL DEBUG JS] Нет сохраненного реферального кода в localStorage для чтения.');
                return 0; // Возвращаем null pointer (0)
            }
        } catch(e) {
            console.error('[REFERAL DEBUG JS] Ошибка при чтении кода из localStorage:', e);
            return 0; // Возвращаем null pointer (0) при ошибке
        }
    },

    // Добавляем функцию для проверки наличия реферального кода в localStorage
    HasSavedReferalCode: function() {
        try {
             var hasCode = !!localStorage.getItem('bottle_game_referal_code'); // Преобразуем в boolean, затем в 1 или 0
             // console.log('[REFERAL DEBUG JS] Проверка наличия кода в localStorage:', hasCode); // Можно раскомментировать для отладки
             return hasCode ? 1 : 0;
        } catch(e) {
            console.error('[REFERAL DEBUG JS] Ошибка при проверке наличия кода в localStorage:', e);
            return 0; // Возвращаем 0 при ошибке
        }
    }
});