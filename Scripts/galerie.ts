﻿/// <reference path='typings/jquery/jquery.d.ts' />

module JMSFotoGalerie {

    export function Initialize(): void {
        // Theoretisch erlauben wir auch mehrere Galieren auf einer Seite
        $('.galerie').each((index, galerie) => new Galerie(galerie));
    }

    // So wird die Konfiguration im Bildverzeichnis ablegt
    class Konfiguration {
        // Die Überschrift über alles
        title: string;

        // Die Namen und Ordnung aller Bilder
        images: string[];
    }

    class Galerie {
        constructor(element: Element) {
            // Bilderverzeichnis
            this.url = element.getAttribute('data-directory') + '/';

            // Das Hüllenelement
            this.element = $(element);
            this.pageBody = $('body');

            // Konfiguration laden
            this.loadIndexFile();
        }

        // Der gesamte Inhalt der Seite
        private pageBody: JQuery;

        // Das Hüllenelement, dessen Inhalt ausgetauscht wird
        private element: JQuery;

        // Der relative oder absolute Pfad zum Bildverzeichnis
        private url: string;

        // Alle Bilder ind er gewünschten Ordnung
        private images: string[];

        // Die Anzahl der Spalten der Detailansicht
        private columns(): number {
            return Math.max(1, Math.floor(this.pageBody.width() / 214));
        }

        // Die Anzahl der Zeilen in der Detailansicht
        private rows(): number {
            // Was am unteren Rand sicher fehlt - geht nur, wenn die Galerie das unterste Anzeigeelement ist
            var spacing = parseInt(this.pageBody.css('padding-bottom')) + parseInt(this.pageBody.css('margin-bottom'));

            // Für die Seitennavigation haben wir eine Höhe von 1em gewählt und ein bisschen Rahmen drumherum
            var navigationHeight = 10 + parseInt(this.pageBody.css('font-size')) + 4;

            // Da stehen wir aber unsere Höhe wird durch den Inhalt bestimmt
            var top = Math.ceil(this.element.position().top);

            // Also wäre das der Rest unseres Platze
            var space = window.innerHeight - top - spacing - navigationHeight;

            return Math.max(1, Math.floor(space / 214));
        }

        // Die Anzahl der Minibilder einer Seite der Detailansicht
        private pageSize(): number {
            return this.rows() * this.columns();
        }

        // Ändern der Anzeige
        private navigate(): void {
            // Voreingestellt ist Seite der Übersicht
            var hash = window.location.hash;
            if (hash.indexOf('#') != 0)
                hash = '#index0';

            // Index auslesen
            var index = +hash.substr(6);

            // Je nach Wunsch zeigen wir eine Übersicht oder ein einzelnes Bild an
            if (hash.indexOf('#index') == 0)
                this.switchToThumbnails(index);
            else if (hash.indexOf('#image') == 0)
                this.showFullImage(index);
        }

        // Konfiguration laden
        private loadIndexFile(): void {
            var request = $.ajax({
                url: this.url + 'galerie.txt',
                dataType: 'json'
            });

            request.done((data: Konfiguration) => {
                // Konfiguration merken
                this.images = data.images;

                // Wir arbeiten vollständig mit dem Anker
                window.onhashchange = window.onresize = (ev: Event) => this.navigate();

                // Überschrift setzen
                $('.galerieTitle').text(data.title);

                // Titel setzen
                $(document).attr("title", data.title);

                // Erste Seite der Übersicht anzeigen
                this.navigate();
            });
        }

        // Zeigt eine einzelne Übersichtsseite an
        private switchToThumbnails(page: number): void {
            this.pageBody.addClass('thumbnailView');
            this.pageBody.removeClass('detailView');
            this.element.empty();

            // Navigation einrichten, wenn nötig
            var pages = this.createPages();
            if (pages != null) {
                // Korrektur für den Fall, dass wir nun weniger Seiten anzeigen
                if (page >= pages.length)
                    page = 0;

                var pageNavigator = $('<div class="detailNavigation pageLinks" />');

                var previous = $('<a href="#index' + Math.max(0, page - 1) + '" class="pageLink navigationText" title="Vorherige Seite">&lt;&lt;</a>');
                var next = $('<a href="#index' + Math.min(pages.length - 1, page + 1) + '" class="pageLink navigationText" title="Nächste Seite">&gt;&gt;</a>');

                pages[page].removeClass('navigationText');
                pages[page].addClass('disabledPageLink');

                pageNavigator.append(previous);
                pageNavigator.append(pages);
                pageNavigator.append(next);

                this.element.append(pageNavigator);
            }

            var startIndex = page * this.pageSize();
            var rows = this.rows();

            // Bildzellenmatrix vorbereiten
            for (var row = 0; row < rows; row++) {
                var cells = this.createThumbs(startIndex);

                this.element.append(cells);

                startIndex += cells.length;
            }
        }

        // Erstellt die seitenweise Navigation
        private createPages(): JQuery[] {
            // Weniger Bilder als auf eine Seite gehen
            var images = this.images.length;
            var pageSize = this.pageSize();
            if (pageSize >= images)
                return null;

            var numPages = Math.floor((images + pageSize - 1) / pageSize);
            var pages: JQuery[] = new Array();

            for (var pageIndex = 0; pageIndex < numPages; pageIndex++) {
                pages[pageIndex] = $('<a href="#index' + pageIndex + '" class="pageLink navigationText">' + (pageIndex + 1) + '</a>');
            }

            return pages;
        }

        // Füllt Minibilder in eine Liste von Zellen
        private createThumbs(startIndex: number): JQuery[] {
            var cells: JQuery[] = new Array();
            var columns = this.columns();
            var images = this.images;

            for (var column = 0; column < columns; column++, startIndex++) {
                var cell = $('<div class="thumbnail" />');

                cells[column] = cell;

                // Füllen ist nur nötig, wenn auch ein Bild existiert
                if (startIndex < 0)
                    continue;
                if (startIndex >= images.length)
                    continue;

                var image = images[startIndex];
                var thumb = this.url + 'tn_' + image;

                cell.first().append($('<a href="#image' + startIndex + '"><img class="thumbnailImage" src="' + thumb + '" /></a>'));
            }

            return cells;
        }

        // Zeigt ein Bild in voller Größe
        private showFullImage(imageIndex: number): void {
            this.pageBody.removeClass('thumbnailView');
            this.pageBody.addClass('detailView');
            this.element.empty();

            var images = this.images;
            var columns = this.columns();
            var rows = this.rows();
            var columnIndex = imageIndex % columns;
            var rowIndex = (imageIndex - columnIndex) / columns;

            var page = Math.floor(imageIndex / (rows * columns));

            var backToIndex = $('<div class="detailNavigation center"><a href="#index' + page + '" class="navigationText">Zurück zur Übersicht</a></div>');
            var visual = $('<div class="fullImageContainer"><img class="fullImage" src="' + this.url + images[imageIndex] + '" /></div>');

            this.element.append($('<div class="thumbnailRow" />').append(this.createThumbs(Math.floor(imageIndex - (columns - 1) / 2))));
            this.element.append(backToIndex);
            this.element.append(visual);
        }
    }
}

// Globale Initialisierungen
$(() => JMSFotoGalerie.Initialize());