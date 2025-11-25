## Zadanie_Rekrutacyjne

Celem zadania było stworzenie aplikacji, która pozwala budować bazę danych FireBird, eksportować skrypty oraz wprowadzać aktualizacje.


## Sposób wywołania:

  build-db --db-dir "<ścieżka>" --scripts-dir "<ścieżka>"
  
  *budowanie bazy danych w określonym katalogu i uruchomienie skryptów w wybranym katalogu;
  
  export-scripts --connection-string "<connStr>" --output-dir "<ścieżka>"
  
  *eksportowanie skryptów z podłączonej bazy danych do wskazanego katalogu;
  
  update-db --connection-string "<connStr>" --scripts-dir "<ścieżka>"
  
  *aktualizuje podłączoną bazę danych, uruchamiając skrypty ze wskazanego katalogu

## Przykłady:

  DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
  
  DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
  
  DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"

## Uwagi

Eksportowanie plików nie uwzględnia wartości domyślnych dla domen oraz nie rozpoznaje typu DECIMAL.

Polecenie "using FirebirdSql.Data.FirebirdClient;" jest konieczne, by móc połączyć się z bazą danych i wprowadzać kolejne polecenia.

Dla poprawnego działania są potrzebne pliki DLL z aplikacji FireBird w katalogu z plikiem wykonywalnym.
