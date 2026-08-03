# Naming Formats

Mnema uses a custom naming format system to generate chapter and one-shot file names. Formats are made up of normal text combined with variables that are replaced with values from the current chapter context.

A format can contain:

* **Variables** using `{VariableName}`
* **Optional sections** using `[ ... ]`

Optional sections are useful when parts of a name should only appear when a value exists.

---

## How the Formatter Works

### Variables

Variables are written using curly braces:

```
{VariableName}
```

When a format is applied, each variable is replaced with the value provided by the formatter.

Example:

```
{Title} - Chapter {Chapter}
```

Could become:

```
Spice and Wolf - Chapter 10
```

Variable names are case-insensitive, so these are equivalent:

```
{Title}
{title}
{TITLE}
```

---

### Variable Specifications

Variables can optionally receive a specification after a colon:

```
{VariableName:spec}
```

The specification is passed to the variable resolver and can change how the value is generated.

Example:

```
{Chapter:#4}
```

The `#4` specification tells the chapter formatter to pad the chapter number to four digits.

Example output:

```
Chapter 0007
```

Specifications are variable-specific. Not every variable supports them.

---

## Optional Sections

Square brackets create optional sections:

```
[optional text]
```

A section is included only when at least one variable inside it has a value.

Example:

```
{Title} Ch. {Chapter:#4} [ - {ChapterTitle}]
```

If the chapter has a title:

```
The Moon on a Rainy Night Ch. 0022 - Doubt
```

If the chapter does not have a title:

```
Spice and Wolf Ch. 0022
```

## Available Naming Options

### Chapter Format

The chapter format controls the filename generated for normal chapter downloads.

| Variable         | Description                                                                                                                      | Specification                                                                        |
|------------------|----------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| `{Title}`        | The title of the series.                                                                                                         | Not supported                                                                        |
| `{Volume}`       | The volume marker for the chapter. Loose-leaf volume markers are ignored and will not produce a value.                           | Not supported                                                                        |
| `{Chapter}`      | The chapter number or chapter marker. Default chapters are omitted when the chapter marker represents the default chapter value. | Supports `#<number>` padding. Example: `{Chapter:#4}` outputs `0007` for chapter 7.  |
| `{ChapterTitle}` | The title of the chapter.                                                                                                        | Not supported                                                                        |
| `{Date}`         | The release date of the chapter. Defaults to `yyyy-MM-dd`.                                                                       | Supports standard date format strings. Example: `{Date:yyyy}` outputs only the year. |

---

### One Shot Format

The one-shot format controls filenames for one-shot releases.

Currently, one-shot formats support the same variables and specifications as the Chapter Format.
