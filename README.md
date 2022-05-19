# WPFTextBoxAutoCompleteDerivative

Extends the WPF `TextBox` control with auto-completion suggestions.  
This is based on Nimgoble's [WPFTextBoxAutoComplete](https://github.com/Nimgoble/WPFTextBoxAutoComplete), and is designed for .NET Core 6  

### Differences
- Shortened and simplified syntax.
- Commit is now triggered when the textbox loses focus, in addition to pressing the commit key.
- Commit key can be changed from enter to another key.

## Usage

 1. Install the package via NuGet

	```
		PM> Install-Package WPFTextBoxAutoCompleteDerivative
	```

 2. Add a reference to the library in the `.xaml` file you want to use it in.

	```csharp
		xmlns:behaviors="clr-namespace:WPFTextBoxAutoComplete;assembly=WPFTextBoxAutoCompleteDerivative"
	```
	
 3. Create a textbox and bind the ***ItemsSource*** property to any type that implements `IEnumerable<String>`.

	```csharp
		<TextBox 
			Width="250"
			HorizontalAlignment="Center"
			Text="{Binding TestText, UpdateSourceTrigger=PropertyChanged}"
			behaviors:AutoComplete.ItemsSource="{Binding TestItems}"
		/>
	```
	
### Properties

| Property               | Type                  | Default             | Description                                                                                             |
|------------------------|-----------------------|---------------------|---------------------------------------------------------------------------------------------------------|
| `ItemsSource`          | `IEnumerable<string>` | `null`              | Sets the source for auto completion suggestions.                                                        |
| `StringComparisonMode` | `StringComparison`    | `OrdinalIgnoreCase` | Changes the string comparison type used when matching user input to possible suggestions.               |
| `Prefix`               | `char?`               | `null`              | When set to a non-null character, that character must be entered by the user for suggestions to appear. |
| `CommitKey`            | `Key`                 | `Enter`             | This changes which key the user should press to select a suggestion.                                    |


### Examples
	
	Case-Sensitive Matching
	```csharp
		<TextBox 
			 Width="250"
			 HorizontalAlignment="Center"
			 Text="{Binding TestText, UpdateSourceTrigger=PropertyChanged}" 
			 behaviors:AutoComplete.ItemsSource="{StaticResource TestData}"
			 behaviors:AutoComplete.StringComparisonMode="Ordinal"
		/>
	```
	
	Requires '@' prefix to show suggestions.
	``` csharp
		<TextBox 
			Width="250"
			HorizontalAlignment="Center"
			Text="{Binding TestText, UpdateSourceTrigger=PropertyChanged}" 
			behaviors:AutoComplete.ItemsSource="{Binding TestItems}"
			behaviors:AutoComplete.Prefix="@"
		/>
	```
	
	
