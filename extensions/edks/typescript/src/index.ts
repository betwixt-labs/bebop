import { CompilerContext, IndentedStringBuilder, commitResult, readContext } from "./kernel";

export function chordCompile() {
  const input = readContext();

  commitResult(generateValidators(input));
}

function generateValidators(context: CompilerContext): string {
  const builder = new IndentedStringBuilder();

  for (const [name, definition] of Object.entries(context.definitions)) {
    if (definition.kind === "struct" && definition.decorators?.some(d => d.identifier === "validator")) {
      builder.appendLine(`export function validate${name}(record: I${name}): void {`);
      builder.indent(2);

      for (const [fieldName, field] of Object.entries(definition.fields)) {
        if (field.type === 'string') {
          const lengthDecorator = field.decorators?.find(d => d.identifier === "length");
          if (lengthDecorator && lengthDecorator.arguments) {
            const { min, max } = lengthDecorator.arguments;

            if (min && min.value !== undefined) {
              builder.appendLine(`if (record.${fieldName}.length < ${min.value}) {`);
              builder.appendLine(`  throw new Error(\`${fieldName} must be at least ${min.value} characters long\`);`);
              builder.appendLine('}');
            }

            if (max && max.value !== undefined) {
              builder.appendLine(`if (record.${fieldName}.length > ${max.value}) {`);
              builder.appendLine(`  throw new Error(\`${fieldName} must be no more than ${max.value} characters long\`);`);
              builder.appendLine('}');
            }
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