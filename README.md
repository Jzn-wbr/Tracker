# Tracker

## Comprendre son temps numérique, simplement.

Tracker est une application Windows native qui aide à comprendre comment le temps est réellement passé sur l’ordinateur. Elle suit l’activité des applications et des sites utilisés, puis la transforme en informations lisibles — sans tableau de bord surchargé.

## Pourquoi Tracker ?

Parce qu’il est difficile de changer une habitude que l’on ne voit pas. Tracker permet de :

- voir immédiatement où part le temps aujourd’hui ;
- comparer son usage actuel à la semaine précédente ;
- repérer les applications et sites qui expliquent les changements ;
- comprendre son rythme de travail au fil de la journée ;
- exporter ses données au format CSV pour les analyser librement.

## Une vue claire de votre activité

### Vue d’ensemble

La vue d’ensemble répond à une question simple : **qu’est-ce que j’utilise actuellement ?** Elle met en avant les applications, les sites et les périodes d’activité les plus importantes, dans une interface desktop sobre et lisible.

### Habitudes

La page Habitudes répond à une autre question : **qu’est-ce qui change dans mon usage ?** Elle compare la semaine en cours à la période équivalente précédente et fait ressortir :

- l’évolution du temps total et de la moyenne quotidienne ;
- les jours qui ont le plus changé ;
- les applications et sites à l’origine des variations ;
- le déplacement du rythme de début, de fin et de pic d’activité.

Les comparaisons utilisent uniquement les données disponibles. Les périodes partielles et les absences de données précédentes sont signalées proprement, sans inventer de métriques.

## Pensé pour rester discret

Tracker privilégie une expérience calme : fond clair, cartes lisibles, contrastes maîtrisés et informations utiles uniquement. Les réglages peuvent être ouverts à la demande depuis l’icône dédiée, puis repliés pour retrouver tout l’espace d’analyse.

Les données d’activité sont conservées localement sur l’ordinateur. Tracker n’a pas besoin d’un compte ou d’un service cloud pour fonctionner.

## Installation

Téléchargez `TrackerSetup.exe` depuis la section **Releases** du dépôt GitHub, puis suivez l’assistant d’installation Windows.

Pour lancer le projet depuis les sources :

```powershell
dotnet build Tracker.sln
dotnet run --project src/Tracker.Desktop/Tracker.Desktop.csproj
```

## Pour les développeurs

Le projet est une application WPF en .NET 8. Les calculs de comparaison de la page Habitudes sont séparés du rendu de l’interface afin de garder une base simple à faire évoluer.

```powershell
dotnet build Tracker.sln --no-restore
```

## Projet en évolution

Tracker est conçu autour d’une idée volontairement simple : rendre l’activité numérique compréhensible en quelques secondes, sans transformer le suivi en une nouvelle source de distraction.

