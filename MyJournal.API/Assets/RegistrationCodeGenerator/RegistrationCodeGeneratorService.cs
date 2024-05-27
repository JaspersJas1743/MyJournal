namespace MyJournal.API.Assets.RegistrationCodeGenerator;

public sealed class RegistrationCodeGeneratorService: IRegistrationCodeGeneratorService
{
	private static readonly char[] AllowedSymbols;
	private const int RegistrationCodeLength = 7;

	static RegistrationCodeGeneratorService()
	{
		IEnumerable<int> numbers = Enumerable.Range(start: 48, count: 10);
		IEnumerable<int> uppercase = Enumerable.Range(start: 65, 26);
		IEnumerable<int> lowercase = Enumerable.Range(start: 97, 26);

		AllowedSymbols = numbers.Concat(second: uppercase).Concat(second: lowercase).Select(selector: num => (char)num).ToArray();
	}

	public string Generate()
		=> String.Concat(values: Random.Shared.GetItems(choices: AllowedSymbols, length: RegistrationCodeLength));
}