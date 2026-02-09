package com.retreatverses.komoran;

import java.util.Set;

import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RestController;

import kr.co.shineware.nlp.komoran.core.Komoran;
import kr.co.shineware.nlp.komoran.model.KomoranResult;
import kr.co.shineware.nlp.komoran.model.Token;

@RestController
public class NounController {
    private static final Set<String> NOUN_TAGS = Set.of("NNG", "NNP", "NNB", "NNBC", "NP", "NR");

    private final Komoran komoran;

    public NounController(Komoran komoran) {
        this.komoran = komoran;
    }

    @PostMapping(path = "/api/noun", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public NounResponse checkNoun(@RequestBody NounRequest request) {
        if (request == null || request.text == null) {
            return NounResponse.failure("단어를 입력해 주세요.");
        }

        var word = request.text.trim();
        if (word.length() < 2) {
            return NounResponse.failure("두 글자 이상의 단어만 판별합니다.");
        }

        KomoranResult result = komoran.analyze(word);
        boolean isNoun = false;
        for (Token token : result.getTokenList()) {
            if (word.equals(token.getMorph()) && NOUN_TAGS.contains(token.getPos())) {
                isNoun = true;
                break;
            }
        }

        return new NounResponse(isNoun, isNoun ? null : "명사가 아닙니다.");
    }

    public static class NounRequest {
        public String text;
    }

    public static class NounResponse {
        public boolean isNoun;
        public String message;

        public NounResponse(boolean isNoun, String message) {
            this.isNoun = isNoun;
            this.message = message;
        }

        public static NounResponse failure(String message) {
            return new NounResponse(false, message);
        }
    }
}
