export function memoize<T extends {}>(fn: () => T): () => T {
  let value: T | undefined;
  return () => (value ??= fn());
}

const singleComment = Symbol("singleComment");
const multiComment = Symbol("multiComment");

const stripWithoutWhitespace = (): string => "";
const stripWithWhitespace = (
  string: string,
  start: number,
  end: number
): string => string.slice(start, end).replace(/\S/g, " ");

const isEscaped = (jsonString: string, quotePosition: number): boolean => {
  let index = quotePosition - 1;
  let backslashCount = 0;

  while (jsonString[index] === "\\") {
    index -= 1;
    backslashCount += 1;
  }

  return Boolean(backslashCount % 2);
};

export function stripJsonComments(
  jsonString: string,
  { whitespace = true, trailingCommas = false } = {}
): string {
  if (typeof jsonString !== "string") {
    throw new TypeError(
      `Expected argument \`jsonString\` to be a \`string\`, got \`${typeof jsonString}\``
    );
  }

  const strip = whitespace ? stripWithWhitespace : stripWithoutWhitespace;

  let isInsideString = false;
  let isInsideComment: symbol | false = false;
  let offset = 0;
  let buffer = "";
  let result = "";
  let commaIndex = -1;

  for (let index = 0; index < jsonString.length; index++) {
    const currentCharacter = jsonString[index];
    const nextCharacter = jsonString[index + 1];

    if (!isInsideComment && currentCharacter === '"') {
      const escaped = isEscaped(jsonString, index);
      if (!escaped) {
        isInsideString = !isInsideString;
      }
    }

    if (isInsideString) {
      continue;
    }

    if (!isInsideComment && currentCharacter + nextCharacter === "//") {
      buffer += jsonString.slice(offset, index);
      offset = index;
      isInsideComment = singleComment;
      index++;
    } else if (
      isInsideComment === singleComment &&
      currentCharacter + nextCharacter === "\r\n"
    ) {
      index++;
      isInsideComment = false;
      buffer += strip(jsonString, offset, index);
      offset = index;
      continue;
    } else if (isInsideComment === singleComment && currentCharacter === "\n") {
      isInsideComment = false;
      buffer += strip(jsonString, offset, index);
      offset = index;
    } else if (!isInsideComment && currentCharacter + nextCharacter === "/*") {
      buffer += jsonString.slice(offset, index);
      offset = index;
      isInsideComment = multiComment;
      index++;
      continue;
    } else if (
      isInsideComment === multiComment &&
      currentCharacter + nextCharacter === "*/"
    ) {
      index++;
      isInsideComment = false;
      buffer += strip(jsonString, offset, index + 1);
      offset = index + 1;
      continue;
    } else if (trailingCommas && !isInsideComment) {
      if (commaIndex !== -1) {
        if (currentCharacter === "}" || currentCharacter === "]") {
          buffer += jsonString.slice(offset, index);
          result += strip(buffer, 0, 1) + buffer.slice(1);
          buffer = "";
          offset = index;
          commaIndex = -1;
        } else if (
          currentCharacter !== " " &&
          currentCharacter !== "\t" &&
          currentCharacter !== "\r" &&
          currentCharacter !== "\n"
        ) {
          buffer += jsonString.slice(offset, index);
          offset = index;
          commaIndex = -1;
        }
      } else if (currentCharacter === ",") {
        result += buffer + jsonString.slice(offset, index);
        buffer = "";
        offset = index;
        commaIndex = index;
      }
    }
  }
  return (
    result +
    buffer +
    (isInsideComment
      ? strip(jsonString, offset, jsonString.length)
      : jsonString.slice(offset))
  );
}
