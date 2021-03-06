﻿$(function () {
    var connection = new signalR.HubConnectionBuilder().withUrl("/hub").build();
    connection.start();

    let playerId;
    let playerName;
    let playerColor;
    let playerOneName;
    let playerTwoName;
    const elements = {
        playground: document.querySelector('.main-playground'),
        board: document.querySelector('#board'),
        statusText: document.querySelector('.status-bar-text'),
        statusCheck: document.querySelector('.status-bar-check-notification'),
        whiteName: document.querySelector('.main-playground-white-name'),
        whitePointsValue: document.querySelector('.white-points-value'),
        whiteMoveHistory: document.querySelector('.main-playground-white-move-history'),
        blackPawnsTaken: document.querySelector('.taken-pieces-black-pawn-value'),
        blackKnightsTaken: document.querySelector('.taken-pieces-black-knight-value'),
        blackBishopsTaken: document.querySelector('.taken-pieces-black-bishop-value'),
        blackRooksTaken: document.querySelector('.taken-pieces-black-rook-value'),
        blackQueensTaken: document.querySelector('.taken-pieces-black-queen-value'),
        blackName: document.querySelector('.main-playground-black-name'),
        blackPointsValue: document.querySelector('.black-points-value'),
        blackMoveHistory: document.querySelector('.main-playground-black-move-history'),
        whitePawnsTaken: document.querySelector('.taken-pieces-white-pawn-value'),
        whiteKnightsTaken: document.querySelector('.taken-pieces-white-knight-value'),
        whiteBishopsTaken: document.querySelector('.taken-pieces-white-bishop-value'),
        whiteRooksTaken: document.querySelector('.taken-pieces-white-rook-value'),
        whiteQueensTaken: document.querySelector('.taken-pieces-white-queen-value'),
        gameChatWindow: document.querySelector('.game-chat-window'),
        lobbyChatWindow: document.querySelector('.game-lobby-chat-window'),
        rooms: document.querySelector('.game-lobby-room-container'),
    }

    $('.game-lobby-input-create-btn').click(function () {
        let name = $('.game-lobby-input-name').val();
        if (name !== "") {
            connection.invoke("CreateRoom", name);
        }
    })

    $(document).on('click', '.game-lobby-room-join-btn', function () {
        let id = $(this).parent().attr('class');
        let name = $('.game-lobby-input-name').val();
        if (name !== "") {
            connection.invoke("JoinRoom", name, id);
        }
    })

    $('.threefold-draw-btn').click(function () {
        connection.invoke("IsThreefoldDraw");
    })

    $('.offer-draw-btn').click(function () {
        let oldText = elements.statusText.innerText;
        let oldColor = elements.statusText.style.color;
        elements.statusText.style.color = "black";
        elements.statusText.innerText = `Draw request sent!`;
        sleep(1500).then(() => {
            elements.statusText.style.color = oldColor;
            elements.statusText.innerText = oldText;
        })
        connection.invoke("OfferDrawRequest");
    })

    $('.resign-btn').click(function () {
        connection.invoke("Resign");
    })

    $('.game-chat-send-btn').click(function () {
        let message = $('.game-chat-input').val();
        if (message !== "") {
            connection.invoke('GameSendMessage', message);
        }
    })

    document.querySelector('.game-chat-input').addEventListener("keyup", function (e) {
        if (e.keyCode === 13) {
            $('.game-chat-send-btn').click();
        }
    });

    $('.game-lobby-chat-send-btn').click(function () {
        let message = $('.game-lobby-chat-input').val();
        if (message !== "") {
            connection.invoke('LobbySendMessage', message);
        }
    })

    document.querySelector('.game-lobby-chat-input').addEventListener("keyup", function (e) {
        if (e.keyCode === 13) {
            $('.game-lobby-chat-send-btn').click();
        }
    });

    connection.on("EnterRoom", function (name) {
        document.querySelector('.game-lobby').style.display = "none";
        elements.playground.style.display = "flex";
        elements.board.style.pointerEvents = "none";
        $('.game-btn').prop("disabled", true);
        elements.whiteName.innerHTML = name;
        elements.blackName.innerHTML = "?";
        elements.statusText.style.color = "red";
        elements.statusText.innerText = "WAITING FOR OPPONENT...";
    })

    connection.on("AddRoom", function (player) {
        let div = document.createElement('div');
        let span = document.createElement("span");
        let button = document.createElement("button");

        span.innerText = `${player.name}`;
        span.classList.add('game-lobby-room-name');
        button.innerText = "Join";
        button.classList.add('game-lobby-room-join-btn', 'game-btn', 'btn');

        div.append(span, button);
        div.classList.add(`${player.id}`);
        elements.rooms.appendChild(div);
    })

    connection.on("ListRooms", function (waitingPlayers) {
        $('.game-lobby-room-container').empty();

        waitingPlayers.forEach(player => {
            let div = document.createElement('div');
            let span = document.createElement("span");
            let button = document.createElement("button");

            span.innerText = `${player.name}`;
            span.classList.add('game-lobby-room-name');
            button.innerText = "Join";
            button.classList.add('game-lobby-room-join-btn', 'game-btn', 'btn');

            div.append(span, button);
            div.classList.add(`${player.id}`);
            elements.rooms.appendChild(div);
        });
    })

    connection.on("PlayerJoined", function (player) {
        playerId = player.id;
    })

    connection.on("Start", function (game) {
        document.querySelector('.game-lobby').style.display = "none";
        elements.playground.style.display = "flex";
        elements.board.style.pointerEvents = "auto";
        $('.game-btn').prop("disabled", false);
        $('.threefold-draw-btn').prop("disabled", true);
        playerColor = (playerId == game.player1.id) ? game.player1.color : game.player2.color;
        playerName = (playerId == game.player1.id) ? game.player1.name : game.player2.name;
        playerOneName = game.player1.name;
        playerTwoName = game.player2.name;
        elements.whiteName.innerHTML = playerOneName;
        elements.blackName.innerHTML = playerTwoName;
        updateStatus(game.movingPlayer.name);
    })

    connection.on("ChangeOrientation", function () {
        board.orientation('black');
    })

    connection.on("BoardMove", function (source, target) {
        board.move(`${source}-${target}`);
    })

    connection.on("BoardSnapback", function (fen) {
        board.position(fen);
    })

    connection.on("BoardSetPosition", function (fen) {
        board.position(fen);
    })

    connection.on("EnPassantTake", function (pawnPosition, target) {
        board.move(`${target}-${pawnPosition}`, `${pawnPosition}-${target}`);
    })

    connection.on("GameOver", function (player, gameOver) {
        elements.statusText.style.color = "purple";
        elements.board.style.pointerEvents = "none";
        switch (gameOver) {
            case 1: {
                elements.statusText.innerText = `CHECKMATE! ${player.name.toUpperCase()} WON THE GAME!`;
                elements.statusCheck.style.display = "none";
            }
                break;
            case 2: elements.statusText.innerText = `STALEMATE!`;
                break;
            case 3: elements.statusText.innerText = `DRAW!`;
                break;
            case 4: elements.statusText.innerText = `DECLARED THREEFOLD REPETITION DRAW BY ${player.name.toUpperCase()}!`;
                break;
            case 5: elements.statusText.innerText = `FIVEFOLD REPETITION DRAW!`;
                break;
            case 6: elements.statusText.innerText = `${player.name.toUpperCase()} RESIGNED!`;
                break;
            case 7: elements.statusText.innerText = `${player.name.toUpperCase()} HAS LEFT. YOU WON!`;
                break;
        }

        $('.game-btn').prop("disabled", true);
    })

    connection.on("ThreefoldAvailable", function (player, isAvailable) {
        if (isAvailable) {
            $('.threefold-draw-btn').prop('disabled', false);
        } else {
            $('.threefold-draw-btn').prop('disabled', true);
        }
    })

    connection.on("CheckStatus", function (type) {
        if (type == 2) {
            elements.statusCheck.style.display = "block";
            elements.statusCheck.innerText = "CHECK!";
        } else {
            elements.statusCheck.style.display = "none";
        }
    })

    connection.on("InvalidMove", function (type) {
        elements.statusText.style.color = "red";
        switch (type) {
            case 3: elements.statusText.innerText = "King is in check!";
                break;
            case 4: elements.statusText.innerText = "Will open a check!";
                break;
            default: elements.statusText.innerText = "Invalid Move!";
                break;
        }
        sleep(1200).then(() => {
            elements.statusText.innerText = "Your Turn!";
            elements.statusText.style.color = "green";
        })
    })

    connection.on("DrawOffered", function (player) {
        let oldText = elements.statusText.innerText;
        let oldColor = elements.statusText.style.color;

        let $yes = document.createElement("button");
        $yes.innerText = "YES";
        $yes.classList.add('draw-offer-yes-btn', 'draw-offer-button', 'btn', 'btn-primary');

        let $no = document.createElement("button");
        $no.innerText = "NO";
        $no.classList.add('draw-offer-no-btn', 'draw-offer-button', 'btn', 'btn-primary');

        elements.statusText.style.color = "black";
        elements.statusText.innerText = `Draw offer by ${player.name}! Do you accept?`;

        let $div = document.createElement("div");
        $div.classList.add('draw-offer-container');
        $div.append($yes, $no);
        elements.statusText.appendChild($div);

        $yes.addEventListener("click", function () {
            connection.invoke("OfferDrawAnswer", true);
            elements.statusText.innerText = oldText;
            elements.statusText.style.color = oldColor;
        });

        $no.addEventListener("click", function () {
            connection.invoke("OfferDrawAnswer", false);
            elements.statusText.innerText = oldText;
            elements.statusText.style.color = oldColor;
        });
    })

    connection.on("DrawOfferRejected", function (player) {
        let oldText = elements.statusText.innerText;
        let oldColor = elements.statusText.style.color;
        elements.statusText.style.color = "black";
        elements.statusText.innerText = `Rejected by ${player.name}!`;
        sleep(1500).then(() => {
            elements.statusText.style.color = oldColor;
            elements.statusText.innerText = oldText;
        })
    })

    connection.on("UpdateTakenFigures", function (movingPlayer, pieceName, points) {
        if (movingPlayer.name == playerOneName) {
            elements.whitePointsValue.innerText = points;
            switch (pieceName) {
                case "Pawn": elements.blackPawnsTaken.innerText++;
                    break;
                case "Knight": elements.blackKnightsTaken.innerText++;
                    break;
                case "Bishop": elements.blackBishopsTaken.innerText++;
                    break;
                case "Rook": elements.blackRooksTaken.innerText++;
                    break;
                case "Queen": elements.blackQueensTaken.innerText++;
                    break;
            }
        } else {
            elements.blackPointsValue.innerText = points;
            switch (pieceName) {
                case "Pawn": elements.whitePawnsTaken.innerText++;
                    break;
                case "Knight": elements.whiteKnightsTaken.innerText++;
                    break;
                case "Bishop": elements.whiteBishopsTaken.innerText++;
                    break;
                case "Rook": elements.whiteRooksTaken.innerText++;
                    break;
                case "Queen": elements.whiteQueensTaken.innerText++;
                    break;
            }
        }
    })

    connection.on("UpdateMoveHistory", function (movingPlayer, moveNotation) {
        let li = document.createElement('li');
        li.classList.add('list-group-item');
        li.innerText = moveNotation;

        if (movingPlayer.name == playerOneName) {
            elements.whiteMoveHistory.appendChild(li);
            if (elements.whiteMoveHistory.getElementsByTagName("li").length > 40) {
                elements.whiteMoveHistory.removeChild(elements.whiteMoveHistory.childNodes[0]);
            }
        } else {
            elements.blackMoveHistory.appendChild(li);
            if (elements.blackMoveHistory.getElementsByTagName("li").length > 40) {
                elements.blackMoveHistory.removeChild(elements.blackMoveHistory.childNodes[0]);
            }
        }
    })

    connection.on("UpdateStatus", function (movingPlayerName) {
        updateStatus(movingPlayerName);
    })

    connection.on("HighlightMove", function (source, target, player) {
        let $source = document.getElementsByClassName("square-" + source);
        let $target = document.getElementsByClassName("square-" + target);
        if (player.name === playerOneName) {
            removeHighlight("black");
            $source[0].className += " highlight-white";
            $target[0].className += " highlight-white";
        } else {
            removeHighlight("white");
            $source[0].className += " highlight-black";
            $target[0].className += " highlight-black";
        }
    })

    connection.on("UpdateGameChat", function (message, player) {
        let isBlack = player.name === playerOneName ? false : true;
        updateChat(message, elements.gameChatWindow, false, isBlack);
        document.querySelector('.game-chat-input').value = "";
    })

    connection.on("UpdateGameChatInternalMeesage", function (message) {
        updateChat(message, elements.gameChatWindow, true, false);
    })

    connection.on("UpdateLobbyChat", function (message) {
        updateChat(message, elements.lobbyChatWindow, false, false);
        document.querySelector('.game-lobby-chat-input').value = "";
    })

    connection.on("UpdateLobbyChatInternalMessage", function (message) {
        updateChat(message, elements.lobbyChatWindow, true, false);
    })

    function updateChat(message, chat, isInternalMessage, isBlack) {
        let li = document.createElement('li');
        li.innerText = `${message}`;
        if (isInternalMessage) {
            li.classList.add('chat-internal-msg', 'chat-msg', 'flex-start');
        } else {
            if (isBlack) {
                li.classList.add('black-chat-msg', 'chat-user-msg', 'chat-msg', 'flex-end');
            } else {
                li.classList.add('white-chat-msg', 'chat-user-msg', 'chat-msg', 'flex-start');
            }
        }

        chat.appendChild(li);
        chat.scrollTop = elements.gameChatWindow.scrollHeight;
    }

    function removeHighlight(color) {
        let $squares = document.querySelectorAll(`.highlight-${color}`);
        for (let i = 0; i < $squares.length; i++) {
            $squares[i].className = $squares[i].className.replace(/\bhighlight\b/g, '');
        }
    }

    function updateStatus(name) {
        if (name == playerName) {
            elements.statusText.innerText = "Your turn!";
            elements.statusText.style.color = "green";
        } else {
            elements.statusText.innerText = `${name}'s turn!`;
            elements.statusText.style.color = "red";
        }
    }

    function sleep(time) {
        return new Promise((resolve) => setTimeout(resolve, time));
    }

    function onDrop(source, target, piece, newPos, oldPos) {
        if (playerColor === 0 && piece.search(/b/) !== -1 ||
            playerColor === 1 && piece.search(/w/) !== -1) {
            return 'snapback';
        }
        if (target.length === 2) {
            let sourceFen = Chessboard.objToFen(oldPos);
            let targetFen = Chessboard.objToFen(newPos);
            connection.invoke("MoveSelected", source, target, sourceFen, targetFen);
        }
    }

    var config = {
        pieceTheme: 'img/chesspieces/wikipedia/{piece}.png',
        draggable: true,
        dropOffBoard: 'snapback',
        showNotation: true,
        onDrop: onDrop,
        moveSpeed: 50,
        position: 'start'
    }

    var board = ChessBoard('board', config);
})
