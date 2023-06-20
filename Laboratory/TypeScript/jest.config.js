module.exports = {
    verbose: true,
    testMatch: ["**/test/*.ts"],
    testPathIgnorePatterns: ["node_modules",".*BebopView.ts"],
    transform: {"^.+\\.(ts|tsx)$": "ts-jest"},
};
