import { CompilerContext, IndentedStringBuilder, commitResult, readContext } from "./kernel";

export function chordCompile() {
  const input = readContext();

  commitResult(generateValidators(input));
}

function generateValidators(context: CompilerContext): string {
  const builder = new IndentedStringBuilder();

  for (const [name, definition] of Object.entries(context.definitions)) {
    if (definition.kind === "struct" && definition.decorators?.validate) {
      builder.appendLine(`export function validate${name}(record: I${name}): void {`);
      builder.indent(2);

      for (const [fieldName, field] of Object.entries(definition.fields)) {
        if (field.type === 'string' && field.decorators?.length?.arguments) {
          const { min, max } = field.decorators.length.arguments;

          if (min?.value !== undefined) {
            builder.appendLine(`if (record.${fieldName}.length < ${min.value}) {`);
            builder.appendLine(`  throw new Error(\`${fieldName} must be at least ${min.value} characters long\`);`);
            builder.appendLine('}');
          }

          if (max?.value !== undefined) {
            builder.appendLine(`if (record.${fieldName}.length > ${max.value}) {`);
            builder.appendLine(`  throw new Error(\`${fieldName} must be no more than ${max.value} characters long\`);`);
            builder.appendLine('}');
          }
        }
      }
      builder.dedent(2);
      builder.appendLine('}');
      builder.appendLine();
    }
  }
  return builder.toString();
}