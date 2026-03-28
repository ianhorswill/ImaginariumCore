{

const COMMA = {
    scope: 'symbol',
    begin: ','
};

const PERIOD = {
    scope: 'symbol',
    begin: '\\.'
};


hljs.registerLanguage('imaginarium', function () {
    return {
        keywords: {
            symbol: ['and', 'or', 'not',
                'can', 'must', 'should',
                'are', 'is', 'be', 'implies',
                'exist',
                'kind', 'way',
                'of', 'from', 'at', 'between', 'with',
                'a', 'A', 'An', 'an',
                '.',
                'has', 'have',
                'mutually', 'exclusive',
                'other',
                'all', 'every', 'most', 'least', 'any',
                'rare', 'common',
                'always', 'itself',
                'plural', 'singular',
                'identified', 'described',
                'mention', 'print',
                'called', 'its',
                'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten',
            ]
        },
        contains: [ COMMA, PERIOD, ]
    }
})
}